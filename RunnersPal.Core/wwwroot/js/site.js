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

function MapRoute(map, pointsFormElement) {
    var self = this;
    self._points = [];
    self._endMarker = null;
    self._map = map;
    self._map.on('click', function(e) {
        self.addPoint(e.latlng);
    });
    self._pointsFormElement = pointsFormElement;
}
MapRoute.prototype.addPoint = function (latlng) {
    var self = this;
    if (self._points.length === 0) {
        // add start point...
        L.marker(latlng, {
            alt: 'Start of route',
            title: 'Start of route',
            icon: L.icon({ iconUrl: '/images/pin-start.png', iconAnchor: [11, 36] })
        }).addTo(map);
        self._points.push(latlng);
        self.updatePointsFormElement();
        return;
    }

    const lastPoint = self._points[self._points.length - 1];
    L.polyline([lastPoint, latlng], {color: '#d866eb'}).addTo(map);

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
MapRoute.prototype.updatePointsFormElement = function() {
    var self = this;
    self._pointsFormElement.val(JSON.stringify(self._points));
}