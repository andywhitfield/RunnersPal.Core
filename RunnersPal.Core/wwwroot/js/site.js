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

class MapRoute {
    constructor(map, pointsFormElement, distanceFormElement, distanceDisplayElement) {
        var self = this;
        self._points = [];
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
        self.updatePointsFormElement();
    }
    addPoint(latlng) {
        console.log('adding point @ ' + latlng);
        var self = this;
        if (self._points.length === 0) {
            // add start point...
            L.marker(latlng, {
                alt: 'Start of route',
                title: 'Start of route',
                icon: L.icon({ iconUrl: '/images/pin-start.png', iconAnchor: [11, 36] })
            }).addTo(self._map);
            self._points.push(latlng);
            self.updatePointsFormElement();
            return;
        }

        const lastPoint = self._points[self._points.length - 1];
        L.polyline([lastPoint, latlng], { color: '#d866eb' }).addTo(self._map);

        let curDistance = self._distance;
        let curPoint = { latitude: lastPoint.lat, longitude: lastPoint.lng };
        const nextPoint = { latitude: latlng.lat, longitude: latlng.lng };
        const bearing = geolib.getRhumbLineBearing(curPoint, nextPoint);
        self._distance += geolib.getDistance(curPoint, nextPoint);
        console.log('route distance: ' + self._distance + 'm');
        while (self._distance >= (self._nextDistanceMarker * 1000)) {
            const distanceToNextMarker = (self._nextDistanceMarker * 1000) - curDistance;
            curDistance += distanceToNextMarker;
            const distanceMarkerPoint = geolib.computeDestinationPoint(curPoint, distanceToNextMarker, bearing);
            console.log('new distance marker @ (' + distanceMarkerPoint.latitude + ',' + distanceMarkerPoint.longitude + ')');
            self._map.addLayer(new L.LabelOverlay([distanceMarkerPoint.latitude, distanceMarkerPoint.longitude], '<span class="rp-distance-marker">' + self._nextDistanceMarker + '</span>'));
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
        self.updatePointsFormElement();
    }
    updatePointsFormElement() {
        var self = this;
        self._pointsFormElement.val(JSON.stringify(self._points));
        self._distanceFormElement.val(self._distance);
        self._distanceDisplayElement.text((self._distance / 1000).toLocaleString(undefined, { maximumFractionDigits: 1 }) + ' km');
    }
}
