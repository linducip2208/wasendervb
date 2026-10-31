var results = [];
// Cari array entries bisnis di respons format baru 2025+ (posisi index bisa geser).
function findEntriesArray(node) {
	try {
		if (!node || !node.length) return [];
		for (var k = 0; k < node.length; k++) {
			var c = node[k];
			if (c && c.length && c.length > 1) {
				var ok = 0;
				for (var j = 0; j < Math.min(3, c.length); j++) {
					var e = c[j];
					if (e && e.length && e[1] && e[1].length && typeof e[1][11] === 'string' && e[1][9] && e[1][9].length > 3) { ok++; }
				}
				if (ok >= 2) return c;
			}
		}
	} catch (e) {}
	return [];
}
// Normalisasi envelope respons lama (/*""*/ + .d) maupun baru ()]}' + array).
function handleMapsPayload(responseBody) {
	try {
		if (!responseBody) return;
		var t = String(responseBody).replace(/^\)\]\}'/, '').replace('/*""*/', '');
		var parsed = JSON.parse(t);
		if (parsed && parsed.d) {
			processResponse(parsed.d.substr(4));
			return;
		}
		if (parsed && parsed.length) {
			processResponse(t);
			return;
		}
	} catch (e) {}
}
function processResponse(values) {
	var data = null;
	try { data = JSON.parse(values)[0][1]; } catch (e) { data = null; }
	//var results = [];
	// Cabang format LAMA saja: tiap item harus array len 15 dgn blok [14].
	if (data != null && data.length && data[0] && data[0].length == 15 && data[0][14]) {
		data = JSON.parse(values)[0][1];
		var amount = data.length;

		for (i = 0; i < amount; i++) {
		try {
			if (data[i].length == 15) {
				result = {}

				/*
				1. company_name - done
				2. website - done
				3. cid - done
				4. review - done
				5. rating_count - done
				6. address - done
				7. pincode - done
				8. subtitle
				9. category - done
				10. state - done
				11. city - done
			*/

				// company_name
				result.Business_name = data[i][14][11];

				// website
				var url = '';
				if (data[i][14][7]) {
					url = data[i][14][7][0];
				}
				result.Website_url = url;


				// phone
				try {
					if (data[i][14][178][0][0]) {
						var phone = data[i][14][178][0][3];
					}
				} catch (e) {
					var phone = '';
				}
				result.Phone = phone;

				// review
				try {
					if (data[i][14][4][8]) {
						var total_review = data[i][14][4][8];
					}
				} catch (e) {
					var total_review = '';
				}
				result.Review = total_review;


				// ratings
				try {
					if (data[i][14][4][7]) {
						var rating = data[i][14][4][7];
					}
				} catch (e) {
					var rating = '';
				}
				result.Rating = rating;

				// address
				var address = ''
				if (data[i][14][39]) {
					address = data[i][14][39];
				}
				result.Address = address;
				try {
					// pincode
					result.pincode = result.address.match(/\b[1000-999999]+\b/g);
					result.pincode = result.pincode ? result.pincode : [];
					result.pincode = result.pincode.length ? result.pincode.pop() : "";
				} catch (e) {
					result.pincode = '';
				}

				// category
				try {
					var category = data[i][14][13][0];
				} catch (e) {
					var category = '';
				}
				result.Category = category;

				// city
				try {
					var last_add = data[i][14][2][data[i][14][2].length - 1];
				} catch (e) {
					var last_add = ''
				}

				try {
					var city = last_add.split(',')[0].trim();
				} catch (e) {
					var city = '';
				}
				result.city = city;

				// state
				try {
					var state = last_add.split(',')[1].replace(result.pincode, '').trim();
				} catch (e) {
					var state = '';
				}

				result.state = state;

				// latitude
				var lat = data[i][14][9][2];
				result.Lat = lat;

				// longitude
				var long = data[i][14][9][3];
				result.Long = long;
				try {
					var image1 = data[i][14][37][0][0][6][0];
					let result_image = image1.substring(0, image1.indexOf("="));
					result.Image1 = result_image;
				} catch (e) {

					result.Image1 = '';
				}


				try {
					var working_hour = '';
					var working_hour_array = data[i][14][34][1];
					for (var p = 0; p < working_hour_array.length; p++) {
						var day = working_hour_array[p][0];
						var time = working_hour_array[p][1][0];
						if (working_hour == '') {
							working_hour = day + '-' + time;
						} else {
							working_hour = working_hour + ',' + day + '-' + time;
						}

					}
					result.Working_hour = working_hour;
				} catch (e) {
					result.Working_hour = '';
				}

				results.push(result);
				try{
					chrome.webview.postMessage(result);
					results=[];
				}
				catch(err)
				{}
			}
		} catch (e) { /* lewati 1 item yg formatnya beda, lanjut item berikut */ }
		}


	}
	// Cabang format BARU: selalu gabung tiap batch yg masuk (scroll = banyak XHR).
	// Dedupe via fid di bawah, jadi batch tumpang-tindih aman.
	{
		console.log("results.length=" + results.length)

		let data=[];
		try{
			var _top = JSON.parse(values);
			data = (_top.length > 64 && _top[64] && _top[64].length) ? _top[64] : findEntriesArray(_top);
			if (!data) { data = []; }
			console.log('data parsed, entries=' + data.length)
		}
		catch(err)
		{ data = []; }

		var amount = data.length;

		for (i = 0; i < amount; i++) {
		try {
			// Lewati baris yg bukan data bisnis (mis. header / iklan)
			if (!data[i] || !data[i][1] || typeof data[i][1][11] !== 'string') { continue; }
			result = {}

			/*
			1. company_name - done
			2. website - done
			3. cid - done
			4. review - done
			5. rating_count - done
			6. address - done
			7. pincode - done
			8. subtitle
			9. category - done
			10. state - done
			11. city - done
		*/

			// company_name
			result.Business_name = data[i][1][11];

			// website
			var url = '';
			if (data[i][1][7]) {
				url = data[i][1][7][0];
			}
			result.Website_url = url;


			// phone (format baru: [1][178][0] = [display, [[display,1],[intl,2]], ..., digit, ...])
			var phone = '';
			try {
				var _ph = data[i][1][178][0];
				if (_ph) {
					if (_ph[1] && _ph[1].length) {
						for (var _pi = 0; _pi < _ph[1].length; _pi++) {
							if (_ph[1][_pi] && _ph[1][_pi][1] === 2 && _ph[1][_pi][0]) { phone = _ph[1][_pi][0]; break; }
						}
					}
					if (!phone && _ph[3]) { phone = _ph[3]; }
					if (!phone && _ph[0]) { phone = _ph[0]; }
				}
			} catch (e) {
				phone = '';
			}
			result.Phone = phone;

			// review
			try {
				if (data[i][1][4][8]) {
					var total_review = data[i][1][4][8];
				}
			} catch (e) {
				var total_review = '';
			}
			result.Review = total_review;


			// ratings
			try {
				if (data[i][1][4][7]) {
					var rating = data[i][1][4][7];
				}
			} catch (e) {
				var rating = '';
			}
			result.Rating = rating;

			// address
			var address = ''
			if (data[i][1][39]) {
				address = data[i][1][39];
			}
			result.Address = address;
			try {
				// pincode
				result.pincode = result.Address.match(/\b[1000-999999]+\b/g);
				result.pincode = result.pincode ? result.pincode : [];
				result.pincode = result.pincode.length ? result.pincode.pop() : "";
			} catch (e) {
				result.pincode = '';
			}

			// category
			try {
				var category = data[i][1][13][0];
			} catch (e) {
				var category = '';
			}
			result.Category = category;

			// city
			try {
				var last_add = data[i][1][2][data[i][1][2].length - 1];
			} catch (e) {
				var last_add = ''
			}

			try {
				var city = last_add.split(',')[0].trim();
			} catch (e) {
				var city = '';
			}
			result.city = city;

			// state
			try {
				var state = last_add.split(',')[1].replace(result.pincode, '').trim();
			} catch (e) {
				var state = '';
			}

			result.state = state;

			// latitude
			var lat = data[i][1][9][2];
			result.Lat = lat;

			// longitude
			var long = data[i][1][9][3];
			result.Long = long;
			try {
				var image1 = data[i][1][37][0][0][6][0];
				let result_image = image1.substring(0, image1.indexOf("="));
				result.Image1 = result_image;
			} catch (e) {

				result.Image1 = '';
			}


			try {
				var working_hour = '';
				var working_hour_array = data[i][1][34][1];
				for (var p = 0; p < working_hour_array.length; p++) {
					var day = working_hour_array[p][0];
					var time = working_hour_array[p][1][0];
					if (working_hour == '') {
						working_hour = day + '-' + time;
					} else {
						working_hour = working_hour + ',' + day + '-' + time;
					}

				}
				result.Working_hour = working_hour;
			} catch (e) {
				result.Working_hour = '';
			}

			try { result.fid = data[i][1][10]; } catch (e) { result.fid = ''; }
			var _dup = false;
			for (var _di = 0; _di < results.length; _di++) {
				if (results[_di].fid && results[_di].fid === result.fid) { _dup = true; break; }
				if (results[_di].Business_name === result.Business_name && results[_di].Phone === result.Phone) { _dup = true; break; }
			}
			if (_dup) { continue; }
			results.push(result);
			console.log(results);
			try{
					chrome.webview.postMessage(result);
				}
				catch(err)
				{}
		} catch (e) { /* lewati 1 item yg formatnya beda */ }
		}


	}

	console.log(results)

	const obj = JSON.stringify(results);
	document.getElementById('fb-group-data').value = obj;
	//return results;
}

function endOfListExists() {
	if (document.querySelectorAll(".fontHeadlineLarge").length) {
		console.log('single page result...');
		return true;
	}
	return document.getElementsByClassName('HlvSq').length;
}

function main() {
	// buildCTABtn();
	var e = document.createElement("input")
	e.setAttribute("type", "hidden");
	e.setAttribute("id", "fb-group-data");
	document.body.appendChild(e)

	var e1 = document.createElement("input")
	e1.setAttribute("type", "hidden");
	e1.setAttribute("id", "fb-start");
	document.body.appendChild(e1);


	let oldXHROpen = window.XMLHttpRequest.prototype.open;
	window.XMLHttpRequest.prototype.open = function (method, url) {
		this.addEventListener("load", function () {
			try {
				if (String(url).indexOf('/search?tbm=map') > -1 && this.status == 200) {
					handleMapsPayload(this.responseText || this.response);
				}
			} catch (e) {
				// bukan respons yg dicari, abaikan
			}
		});
		return oldXHROpen.apply(this, arguments);
	};
	// Fallback kalau Maps pakai fetch
	try {
		if (window.fetch && !window.fetch.__mapsHooked) {
			var _fetchOrig = window.fetch;
			window.fetch = function (u, o) {
				var pr = _fetchOrig.apply(this, arguments);
				try {
					if (String(u).indexOf('/search?tbm=map') > -1) {
						pr.then(function (r) { return r.clone().text(); }).then(function (t) { try { handleMapsPayload(t); } catch (e) {} }).catch(function () {});
					}
				} catch (e) {}
				return pr;
			};
			window.fetch.__mapsHooked = true;
		}
	} catch (e) {}


}
(window.members_list = window.members_list || [["Profile Id", "Full Name", "ProfileLink", "Bio", "Image Src", "Groupe Id", "Group Joining Text", "Profile Type"]]), main();
var initial = 0;
var previousheight = 0;
var tryfor = 0;
//var myTimeout = setTimeout(ScrollWeb(), 1000);


function ScrollWeb() {
	console.log('call scroll web');
	console.log(document.body.scrollHeight);
	console.log(previousheight);
	myTimeout = setTimeout(ScrollWeb(), 1000);

}

// Version 1.0.8