function rpInitialise() {
    $(window).resize(function() {
        $('aside').css('display', '');
        if ($('.nav-close:visible').length > 0) {
            $('.nav-close').css('display', '');
            $('.nav-show').css('display', '');
        }
    });
    $('.nav-show').click(function() {
        $('aside').fadeToggle('fast');
        $(this).hide();
        $('.nav-close').show();
    });
    $('.nav-close').click(function() {
        $('aside').hide();
        $(this).hide();
        $('.nav-show').show();
    });
    $('[data-href]').click(function(e) {
        window.location.href = $(this).attr('data-href');
        e.preventDefault();
        return false;
    });
    $('[data-confirm]').click(function(event) {
        if (!confirm($(this).attr('data-confirm'))) {
            event.preventDefault();
            return false;
        }
    });
    $('[data-depends]').each(function() {
        let btnWithDependency = $(this);
        let dependentFormObject = $(btnWithDependency.attr('data-depends'));
        if (dependentFormObject.is('input')) {
            dependentFormObject.on('keypress', function(e) {
                if (btnWithDependency.attr('disabled') && (e.keyCode || e.which) === 13) {
                    e.preventDefault();
                    return false;
                }
            });
        }
        dependentFormObject.on('change input paste keyup pickmeup-change', function() {
            let dependentValue = $(this).val();
            btnWithDependency.prop('disabled', dependentValue === null || dependentValue.match(/^\s*$/) !== null);
        });
        dependentFormObject.trigger('change');
    });
}

function rpCalculateDistance(src, kmEl, milesEl, onSuccess) {
    console.log('Calculating distance for ' + src + ' km: ' + kmEl.val() + ' mile: ' + milesEl.val());
    $.getJSON('/api/calculator/distance', {
        km: kmEl.val(),
        mile: milesEl.val(),
        source: src
    })
    .done(function(data) {
        console.log('distances returned: ' + data.km + 'km / ' + data.mile + 'mile');
        if (src === 'mile') {
            kmEl.val(data.km);
        } else if (src === 'km') {
            milesEl.val(data.mile);
        } else {
            kmEl.val(data.km);
            milesEl.val(data.mile);
        }
        if (typeof onSuccess !== 'undefined')
            onSuccess();
    })
    .fail(function() {
        console.warn('failed calculating distances, probably invalid input, showing zeros');
        if (src === 'mile') {
            kmEl.val('0');
        } else if (src === 'km') {
            milesEl.val('0');
        } else {
            kmEl.val('0');
            milesEl.val('0');
        }
    });
}

coerceToArrayBuffer = function (thing, name) {
    if (typeof thing === "string") {
        // base64url to base64
        thing = thing.replace(/-/g, "+").replace(/_/g, "/");

        // base64 to Uint8Array
        var str = window.atob(thing);
        var bytes = new Uint8Array(str.length);
        for (var i = 0; i < str.length; i++) {
            bytes[i] = str.charCodeAt(i);
        }
        thing = bytes;
    }

    // Array to Uint8Array
    if (Array.isArray(thing)) {
        thing = new Uint8Array(thing);
    }

    // Uint8Array to ArrayBuffer
    if (thing instanceof Uint8Array) {
        thing = thing.buffer;
    }

    // error if none of the above worked
    if (!(thing instanceof ArrayBuffer)) {
        throw new TypeError("could not coerce '" + name + "' to ArrayBuffer");
    }

    return thing;
};

coerceToBase64Url = function (thing) {
    // Array or ArrayBuffer to Uint8Array
    if (Array.isArray(thing)) {
        thing = Uint8Array.from(thing);
    }

    if (thing instanceof ArrayBuffer) {
        thing = new Uint8Array(thing);
    }

    // Uint8Array to base64
    if (thing instanceof Uint8Array) {
        var str = "";
        var len = thing.byteLength;

        for (var i = 0; i < len; i++) {
            str += String.fromCharCode(thing[i]);
        }
        thing = window.btoa(str);
    }

    if (typeof thing !== "string") {
        throw new Error("could not coerce to string");
    }

    // base64 to base64url
    // NOTE: "=" at the end of challenge is optional, strip it off here
    thing = thing.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");

    return thing;
};

L.LabelOverlay = L.Layer.extend({
    initialize: function(/*LatLng*/ latLng, /*String*/ label, options) {
        this._latlng = latLng;
        this._label = label;
        L.Util.setOptions(this, options);
    },
    options: {
        offset: new L.Point(0, 2)
    },
    onAdd: function(map) {
        this._map = map;
        if (!this._container) {
            this._initLayout();
        }
        map.getPanes().popupPane.appendChild(this._container);
        this._container.innerHTML = this._label;
        map.on('movestart', this._update_start, this);
        map.on('moveend', this._update_end, this);
        this._update_end();
    },
    onRemove: function(map) {
        map.getPanes().popupPane.removeChild(this._container);
        map.off('movestart', this._update_start, this);
        map.off('moveend', this._update_end, this);
    },
    _update_start: function(){
        L.DomUtil.setPosition(this._container, 0);
    },
    _update_end: function() {
        var pos = this._map.latLngToLayerPoint(this._latlng);
        var op = new L.Point(pos.x + this.options.offset.x, pos.y - this.options.offset.y);
        L.DomUtil.setPosition(this._container, op);
    },
    _initLayout: function() {
        this._container = L.DomUtil.create('div', 'leaflet-label-overlay');
    }
});

function rpInitMap(map) {
    L.DomUtil.addClass(map._container,'crosshair-cursor-enabled');
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);
}
function rpSetMapDefaultView(map) {
    if (!navigator.geolocation) {
        map.setView([54.505, -0.09], 6);
    } else {
        navigator.geolocation.getCurrentPosition(
            function(position) {
                map.setView([position.coords.latitude, position.coords.longitude], 12);
            },
            function() {
                map.setView([54.505, -0.09], 6);
            });
    }
}

class MapRoute {
    constructor(map, pointsFormElement, distanceFormElement, distanceDisplayElement, userUnits, metersToUserUnitMultiplier) {
        var self = this;
        self._userUnits = userUnits;
        self._metersToUserUnitMultiplier = metersToUserUnitMultiplier;
        self._points = [];
        self._mapPoints = []; // the map layer/line elements
        self._startMarker = null;
        self._endMarker = null;
        self._map = map;
        self._map.on('click', function (e) {
            self.addPoint(e.latlng);
        });
        self._pointsFormElement = pointsFormElement;
        self._distance = 0;
        self._distanceFormElement = distanceFormElement;
        self._distanceDisplayElement = distanceDisplayElement;
        self._nextDistanceMarker = 1;
        self._changeCallbacks = [];
        self.updatePointsFormElement();
    }
    onChange(callback) {
        var self = this;
        self._changeCallbacks.push(callback);
    }
    addPoint(latlng) {
        console.log('adding point @ ' + latlng);
        var self = this;
        if (self._startMarker === null) {
            self._startMarker = L.marker(latlng, {
                alt: 'Start of route',
                title: 'Start of route',
                icon: L.icon({ iconUrl: '/images/pin-start.png', iconAnchor: [11, 36] })
            }).addTo(self._map);
            self._points.push(latlng);
            self.updatePointsFormElement();
            return;
        }

        const lastPoint = self._points[self._points.length - 1];
        const newLine = L.polyline([lastPoint, latlng], { color: '#d866eb' }).addTo(self._map);
        let distanceMarkers = [];

        let curDistance = self._distance;
        let curPoint = { latitude: lastPoint.lat, longitude: lastPoint.lng };
        const nextPoint = { latitude: latlng.lat, longitude: latlng.lng };
        const bearing = geolib.getRhumbLineBearing(curPoint, nextPoint);
        self._distance += geolib.getDistance(curPoint, nextPoint);
        console.log('route distance: ' + self._distance + 'm');
        while (self._distance >= (self._nextDistanceMarker * self._metersToUserUnitMultiplier)) {
            const distanceToNextMarker = (self._nextDistanceMarker * self._metersToUserUnitMultiplier) - curDistance;
            curDistance += distanceToNextMarker;
            const distanceMarkerPoint = geolib.computeDestinationPoint(curPoint, distanceToNextMarker, bearing);
            console.log('new distance marker @ (' + distanceMarkerPoint.latitude + ',' + distanceMarkerPoint.longitude + ')');
            const distanceMarkerLayer = new L.LabelOverlay([distanceMarkerPoint.latitude, distanceMarkerPoint.longitude], '<span class="rp-distance-marker">' + self._nextDistanceMarker + '</span>');
            self._map.addLayer(distanceMarkerLayer)
            distanceMarkers.push(distanceMarkerLayer);
            self._nextDistanceMarker++;
            curPoint = distanceMarkerPoint;
        }

        if (self._endMarker === null) {
            self._endMarker = L.marker(latlng, {
                alt: 'End of route',
                title: 'End of route',
                icon: L.icon({ iconUrl: '/images/pin-end.png', iconAnchor: [11, 36] })
            }).addTo(map);
        } else {
            self._endMarker.setLatLng(latlng);
        }

        self._points.push(latlng);
        self._mapPoints.push({ line: newLine, markers: distanceMarkers });
        self.updatePointsFormElement();
    }
    undoLastPoint() {
        var self = this;
        if (self._mapPoints.length > 0) {
            console.log('removing last point');
            self._points.pop();
            const lastMapPoint = self._mapPoints.length > 0 ? self._mapPoints.pop() : null;

            if (lastMapPoint !== null) {
                const lastMapPointFrom = { latitude: lastMapPoint.line.getLatLngs()[0].lat, longitude: lastMapPoint.line.getLatLngs()[0].lng };
                const lastMapPointTo = { latitude: lastMapPoint.line.getLatLngs()[1].lat, longitude: lastMapPoint.line.getLatLngs()[1].lng };
                self._distance -= geolib.getDistance(lastMapPointFrom, lastMapPointTo);
                self._nextDistanceMarker -= lastMapPoint.markers.length;

                console.log('removed ' + lastMapPoint.markers.length + ' distance markers and reduced distance to ' + self._distance);

                lastMapPoint.line.remove();
                for (const distanceMarker of lastMapPoint.markers) {
                    distanceMarker.remove();
                }
            }

            if (self._endMarker !== null) {
                if (self._mapPoints.length === 0) {
                    console.log('removing end marker');
                    self._endMarker.remove();
                    self._endMarker = null;
                } else {
                    console.log('moving end marker to ' + self._points[self._points.length - 1]);
                    self._endMarker.setLatLng(self._points[self._points.length - 1]);
                }
            }
        } else {
            self._points.pop();
            if (self._startMarker !== null) {
                console.log('removing start marker');
                self._startMarker.remove();
                self._startMarker = null;
            }
        }
        self.updatePointsFormElement();
    }
    clearRoute() {
        var self = this;
        if (self._endMarker !== null) {
            self._endMarker.remove();
            self._endMarker = null;
        }
        if (self._startMarker !== null) {
            self._startMarker.remove();
            self._startMarker = null;
        }
        for (const mapPoint of self._mapPoints) {
            mapPoint.line.remove();
            for (const distanceMarker of mapPoint.markers) {
                distanceMarker.remove();
            }
        }
        self._points.length = 0;
        self._mapPoints.length = 0;
        self._distance = 0;
        self._nextDistanceMarker = 1;
        self.updatePointsFormElement();
    }
    fitBounds(points) {
        var self = this;
        self._map.fitBounds(L.latLngBounds(points));
    }
    updatePointsFormElement() {
        var self = this;
        self._pointsFormElement.val(JSON.stringify(self._points));
        self._distanceFormElement.val(self._distance);
        self._distanceDisplayElement.text((self._distance / self._metersToUserUnitMultiplier).toLocaleString(undefined, { maximumFractionDigits: 1 }) + self._userUnits);

        for (const changeCallback of self._changeCallbacks) {
            changeCallback();
        }
    }
}
