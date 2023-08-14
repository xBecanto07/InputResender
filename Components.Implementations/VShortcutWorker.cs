using System;
using System.Collections.Generic;
using Components.Interfaces;
using Components.Library;
using ModE = Components.Interfaces.InputData.Modifier;

namespace Components.Implementations {
	public class VShortcutWorker : DShortcutWorker {
		readonly Dictionary<ModE, Dictionary<KeyCode, List<ShortcutInfo>>> CallbackDict;

		public VShortcutWorker ( CoreBase newOwner ) : base ( newOwner ) {
			CallbackDict = new Dictionary<ModE, Dictionary<KeyCode, List<ShortcutInfo>>> ();
		}

		public override int ComponentVersion => 1;

		public override bool Exec ( InputData inputData ) => Exec ( inputData.Key, inputData.Modifiers );
		public override void Register ( KeyCode key, ModE mod, Action callback, string description ) {
			if ( callback == null ) throw new ArgumentNullException ( nameof ( callback ) );
			if ( description == null | description == "" ) throw new ArgumentNullException ( nameof ( description ) );

			if ( !CallbackDict.ContainsKey ( mod ) )
				CallbackDict.Add ( mod, new Dictionary<KeyCode, List<ShortcutInfo>> () );
			var keyDict = CallbackDict[mod];
			if ( !keyDict.ContainsKey ( key ) )
				keyDict.Add ( key, new List<ShortcutInfo> () );
			var info = new ShortcutInfo () { Action = callback, Description = description, Key = key, Modifier = mod };
			keyDict[key].Add ( info );
		}
		public override void Unregister ( KeyCode key, ModE mod, Action callback ) {
			if ( !CallbackDict.ContainsKey ( mod ) ) return;
			var keyDict = CallbackDict[mod];
			if ( !keyDict.ContainsKey ( key ) ) return;
			keyDict[key].RemoveAll ( ( info ) => info.Action == callback );
			if ( keyDict[key].Count == 0 ) keyDict.Remove ( key );
			if ( CallbackDict[mod].Count == 0 ) CallbackDict.Remove ( mod );
		}

		public bool Exec ( KeyCode key, ModE mod ) {
			bool ret = false;
			if ( CallbackDict.TryGetValue ( mod, out var keyDict ) )
				if ( keyDict.TryGetValue ( key, out var actList ) ) {
					foreach ( var act in actList ) {
						ret = true;
						act.Action ();
					}
				}
			return ret;
		}

		public override StateInfo Info => new VStateInfo ( this );
		public class VStateInfo : DStateInfo {
			public new VShortcutWorker Owner => (VShortcutWorker)base.Owner;
			public VStateInfo (VShortcutWorker owner) : base ( owner ) { }

			protected override string[] GetShortcuts () {
				List<string> ret = new List<string> ();
				foreach ( var modSCs in Owner.CallbackDict ) {
					foreach ( var CBs in modSCs.Value) {
						foreach ( var CB in CBs.Value )
							ret.Add ( $"({CB.Modifier}, {CB.Key}) => {CB.Action.Method.AsString ()} ({CB.Description})" );
					}
				}
				return ret.ToArray ();
			}
		}
	}
}