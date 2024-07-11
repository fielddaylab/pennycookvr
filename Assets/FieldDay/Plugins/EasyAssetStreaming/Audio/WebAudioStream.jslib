var WebAudioStreamLibrary = {
    
    $WAS: {
        /**
         * Streaming element
         * @property {number} index    Index
         * @property {number} magic    Magic value
         * @property {number} id       Full identifier
         * @property {Audio} resource Inner Audio resource
         * @property {boolean} active   Whether or not this element has been allocated
        */
        StreamElement : function(index) {
            this.index = index | 0;
            this.magic = 0;
            this.id = 0;

            this.resource = new Audio();
            this.active = false;
            this.playing = false;

            const self = this;

            this.resource.autoplay = false;
            this.resource.controls = false;
            this.resource.disableRemotePlayback = true;

            this.resource.onplay = this.resource.onplaying = function() {
                self.playing = true;
            };
            this.resource.onpause = this.resource.onended = function() {
                self.playing = false;
            };
        },

        /** @type {StreamElement[]} */
        ElementPool: null,

        /**
         * Returns the element in the pool.
         * @param {number} id 
         * @returns {StreamElement}
         */
        GetActiveElement: function(id) {

            if (id == 0) {
                return null;
            }

            const index = WAS.ExtractIndex(id);
            const pool = WAS.ElementPool;

            if (index < 0 || index >= pool.length) {
                console.error("[WASStreamLibrary] Invalid Stream Id " + id);
                return null;
            }

            const element = pool[index];
            if (!element.active || element.id != id) {
                return null;
            }

            return element;
        },

        /**
         * Constructs a stream id.
         * @param {number} index
         * @param {number} magic
         * @return {number}
         */
        ConstructId: function(index, magic) {
            index = index & 16777215;
            magic = magic & 255;
            return index << 8 | magic;
        },

        /**
         * @param {number} id
         * @return {number}
        */
        ExtractIndex: function(id) {
            return (id >> 8) & 16777215;
        }
    },

    /**
     * @param {number} poolSize Initial size of streaming pool
     * @returns {boolean}
     */
    WASInit: function (poolSize) {
        if (WAS.ElementPool) {
            return false;
        }

        const elementPool = WAS.ElementPool = new Array(poolSize);

        for(var i = 0; i < poolSize; i++) {
            elementPool[i] = new WAS.StreamElement(i);
        }

        const mediaSession = navigator.mediaSession;
        if (mediaSession) {
            const emptyFunction = function() { }
            mediaSession.setActionHandler('play', emptyFunction);
            mediaSession.setActionHandler('pause', emptyFunction);
            mediaSession.setActionHandler('stop', emptyFunction);
            mediaSession.setActionHandler('seekbackward', emptyFunction);
            mediaSession.setActionHandler('seekforward', emptyFunction);
            mediaSession.setActionHandler('seekto', emptyFunction);
            mediaSession.setActionHandler('previoustrack', emptyFunction);
            mediaSession.setActionHandler('nexttrack', emptyFunction);
        }

        return true;
    },

    // #region Alloc/Free

    /**
     * Allocates a new stream element.
     * @param {string} path The path for the audio source
     * @returns {number}    The identifier for the new stream
     */
     WASStreamAlloc: function(path) {
        /** @type {StreamElement} **/
        var element = null;

        const pool = WAS.ElementPool;
        const elementCount = pool.length;
        
        for(var i = 0; i < elementCount; i++) {
            if (!pool[i].active) {
                element = pool[i];
                break;
            }
        }

        if (!element) {
            element = new WAS.StreamElement(elementCount);
            pool.push(element);
        }

        element.resource.src = UTF8ToString(path);;
        element.resource.volume = 1;
        element.resource.loop = false;
        element.resource.muted = false;
        element.playing = false;
        element.active = true;

        element.magic = (element.magic + 1) % 256;
        return (element.id = WAS.ConstructId(element.index, element.magic));
    },

    /**
     * Frees the given stream
     * @param {number} id Identifier for the stream
     * @return {boolean}    Whether or not the stream could be freed.
     */
     WASStreamFree: function(id) {
        
        const element = WAS.GetActiveElement(id);
        if (!element)
            return false;

        element.resource.pause();
        element.resource.src = null;
        element.active = false;
        return true;
    },

    // #endregion Alloc/Free

    // #region State

    /**
     * Returns if the stream is ready to play.
     * @param {number} id 
     * @returns {boolean}   If the stream is ready to play.
     */
    WASStreamIsReady: function(id) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        return element.resource.readyState == 4; // HAVE_ENOUGH_DATA
    },

    /**
     * Returns if there are any errors for the given stream
     * @param id 
     * @returns {int}   The error code for the stream
     */
    WASStreamGetError: function(id) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        var error = element.resource.error;
        if (error)
            return error.code;
        return 0;
    },

    /**
     * Returns the duration of the stream.
     * @param {number} id 
     * @returns {number}   The duration of the stream
     */
     WASStreamGetDuration: function(id) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return 0;
        }

        return element.resource.duration;
    },

    /**
     * Returns if the stream is playing.
     * @param {number} id 
     * @returns {boolean}   If the stream is playing.
     */
     WASStreamIsPlaying: function(id) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        return element.playing && !element.resource.paused;
    },

    // #endregion State

    // #region Parameters

    /**
     * Returns if the stream is muted or not.
     * @param {number} id 
     * @returns {boolean} If the stream is muted
     */
     WASStreamGetMute: function(id) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        return element.resource.muted;
    },

    /**
     * Sets whether or not a stream is muted.
     * @param {number} id 
     * @param {boolean} mute 
     * @returns {boolean} Whether or not the operation was successful
     */
    WASStreamSetMute: function(id, mute) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        element.resource.muted = mute;
        return true;
    },

    /**
     * Returns if the stream is looping or not.
     * @param {number} id 
     * @returns {boolean} If the stream is looping
     */
     WASStreamGetLoop: function(id) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        return element.resource.loop;
    },

    /**
     * Sets whether or not a stream loops.
     * @param {number} id 
     * @param {boolean} loop 
     * @returns {boolean} Whether or not the operation was successful
     */
    WASStreamSetLoop: function(id, loop) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        element.resource.loop = loop;
        return true;
    },

    /**
     * Returns the volume of the stream
     * @param {number} id 
     * @returns {number}
     */
     WASStreamGetVolume: function(id) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return 0;
        }

        return element.resource.volume;
    },

    /**
     * Sets the volume of a stream
     * @param {number} id 
     * @param {number} volume 
     * @returns {boolean} Whether or not the operation was successful
     */
    WASStreamSetVolume: function(id, volume) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        element.resource.volume = volume;
        return true;
    },

    // TODO: Implement panning

    /**
     * Returns the current position of the stream
     * @param {number} id
     * @returns {number} The current position, in seconds
     */
    WASStreamGetPosition: function(id) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return 0;
        }

        return element.resource.currentTime;
    },

    /**
     * Sets the position of a stream
     * @param {number} id 
     * @param {number} position 
     * @returns {boolean} Whether or not the operation was successful
     */
     WASStreamSetPosition: function(id, position) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        element.resource.currentTime = position;
        return true;
    },

    // #endregion Parameters

    // #region Operations

    /**
     * Plays the stream.
     * @param {number} id
     * @param {boolean} reset
     * @returns If the operation was successful
     */
    WASStreamPlay: function(id, reset) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        if (reset) {
            element.resource.currentTime = 0;
        }

        element.resource.play();
        return true;
    },

    /**
     * Pauses the stream.
     * @param {number} id
     * @returns If the operation was successful
     */
     WASStreamPause: function(id) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        element.resource.pause();
        return true;
    },

    /**
     * Stops the stream.
     * @param {number} id
     * @returns If the operation was successful
     */
     WASStreamStop: function(id) {
        const element = WAS.GetActiveElement(id);
        if (!element) {
            return false;
        }

        element.resource.pause();
        element.resource.currentTime = 0;
        return true;
    }

    // #endregion Operations
};

autoAddDeps(WebAudioStreamLibrary, "$WAS");
mergeInto(LibraryManager.library, WebAudioStreamLibrary);