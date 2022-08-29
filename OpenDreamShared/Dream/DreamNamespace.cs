using System.Collections.Generic;

namespace OpenDreamShared.Dream {
	public class DreamNamespace{
		public static readonly DreamNamespace World = new DreamNamespace("");
		private static Dictionary<string,DreamNamespace> namespaces = new Dictionary<string,DreamNamespace>();


		public string name {get; private set;}

		public static DreamNamespace GetNamespace(string name){
			if (namespaces.ContainsKey(name)){
				return namespaces[name];
			}
			DreamNamespace space = new DreamNamespace(name);
			namespaces.Add(name,space);
			return space;
		}

		private DreamNamespace(string _name){
			name = _name;
		}

		public override string ToString(){
			return "DreamNamespace(" + name + ")";
		}
	}
}
