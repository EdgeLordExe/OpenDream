using System;
using System.Collections.Generic;
using System.Text;
using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs {
    public class NativeProc : DreamProc {
        public delegate DreamValue HandlerFn(DreamValue src, DreamValue usr, DreamProcArguments arguments);

        public class State : ProcState
        {
            public DreamValue Src;
            public DreamValue Usr;
            public DreamProcArguments Arguments;

            private NativeProc _proc;
            public override DreamProc Proc => _proc;

            public State(NativeProc proc, DreamThread thread, DreamValue src, DreamValue usr, DreamProcArguments arguments)
                : base(thread)
            {
                _proc = proc;
                Src = src;
                Usr = usr;
                Arguments = arguments;
            }

            protected override ProcStatus InternalResume()
            {
                Result = _proc.Handler.Invoke(Src, Usr, Arguments);

                return ProcStatus.Returned;
            }

            public override void AppendStackFrame(StringBuilder builder)
            {
                if (_proc == null) {
                    builder.Append("<anonymous proc>");
                    return;
                }

                builder.Append($"{_proc.Name}(...)");
            }
        }

        private Dictionary<string, DreamValue> _defaultArgumentValues;
        public HandlerFn Handler { get; }

        public NativeProc(string name, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes, Dictionary<string, DreamValue> defaultArgumentValues, HandlerFn handler)
            : base(name, superProc, true, argumentNames, argumentTypes)
        {
            _defaultArgumentValues = defaultArgumentValues;
            Handler = handler;
        }

        public override State CreateState(DreamThread thread, DreamValue src, DreamValue usr, DreamProcArguments arguments)
        {
            if (_defaultArgumentValues != null) {
                foreach (KeyValuePair<string, DreamValue> defaultArgumentValue in _defaultArgumentValues) {
                    int argumentIndex = ArgumentNames.IndexOf(defaultArgumentValue.Key);

                    if (arguments.GetArgument(argumentIndex, defaultArgumentValue.Key) == DreamValue.Null) {
                        arguments.NamedArguments.Add(defaultArgumentValue.Key, defaultArgumentValue.Value);
                    }
                }
            }

            return new State(this, thread, src, usr, arguments);
        }

        public static ProcState CreateAnonymousState(DreamThread thread, HandlerFn handler) {
            return new State(null, thread, DreamValue.Null, DreamValue.Null, new DreamProcArguments(null));
        }
    }
}
