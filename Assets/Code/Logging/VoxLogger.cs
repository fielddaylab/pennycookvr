using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Vox;
using UnityEngine;

namespace Pennycook {
    public class VoxLogger : BatchedComponent, IRegistrationCallbacks
    {
        [SerializeField]
        VoxEmitter _emitter;

        void IRegistrationCallbacks.OnRegister() {
            _emitter.OnStartSpeaking.Register(LogStartVO);
            _emitter.OnFinishSpeaking.Register(LogEndVO);
        }

        void IRegistrationCallbacks.OnDeregister() {
            _emitter.OnStartSpeaking.Deregister(LogStartVO);
            _emitter.OnFinishSpeaking.Deregister(LogEndVO);
        }

        void LogStartVO(VoxRequestHandle h, StringHash32 lineCode)
        {
            Data.PennycookAnalytics pa = Find.State<Data.PennycookAnalytics>();
            if(pa) {
                pa.LogAudioStarted(h, lineCode);
            }
        }

        void LogEndVO(VoxRequestHandle h, StringHash32 lineCode)
        {
            Data.PennycookAnalytics pa = Find.State<Data.PennycookAnalytics>();
            if(pa) {
                pa.LogAudioComplete(h, lineCode);
            }
        }
    }
}
