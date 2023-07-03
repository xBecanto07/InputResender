﻿using Components.Library;
using System.Collections.Generic;
using System;
using MsgType = System.String;
using System.Diagnostics;

namespace Components.Interfaces {
	public abstract class DLogger : ComponentBase<CoreBase> {
		protected DLogger ( CoreBase newOwner ) : base ( newOwner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(Log), typeof(void)),
				(nameof(ReadAt), typeof(MsgType)),
				(nameof(Read), typeof(MsgType[])),
				(nameof(Print), typeof(void)),
				(nameof(Clear), typeof(void))
			};

		public abstract void Log ( string msg );
		public abstract MsgType ReadAt ( int index );
		public abstract MsgType[] Read ( int N );
		public abstract void Print ( Action<MsgType> act );
		public abstract void Clear ();
	}

	public class VLogger : DLogger {
		protected readonly List<MsgType> MsgList;
		protected readonly int MaxMsgs;
		protected ulong msgID = 0;

		public VLogger ( CoreBase newOwner, int maxMessages = 64 ) : base ( newOwner ) {
			MaxMsgs = maxMessages;
			MsgList = new List<MsgType> ( MaxMsgs );
		}

		public override int ComponentVersion => 1;

		public override void Clear () { lock ( MsgList ) { MsgList.Clear (); } }
		public override void Log ( string msg ) {
			lock (MsgList) {
				MsgType msgStr = CreateLogMsg ( msg, msgID++ );
				if ( MsgList.Count >= MaxMsgs ) MsgList.RemoveAt ( MaxMsgs - 1 );
				MsgList.Insert ( 0, msgStr );
			}
		}

		public override void Print ( Action<string> act ) {
			lock ( MsgList ) {
				int N = MsgList.Count;
				for ( int i = 0; i < N; i++ ) act ( MsgList[i] );
			}
		}
		public override MsgType[] Read ( int N ) {
			lock ( MsgList ) {
				return MsgList.GetRange ( 0, N < 0 ? MsgList.Count - N : N ).ToArray ();
			}
		}
		public override MsgType ReadAt ( int index ) {
			lock ( MsgList ) {
				return MsgList[index];
			}
		}

		private MsgType CreateLogMsg (string origMsg, ulong ID) {
			return $"#.{ID.ToShortCode ()}.{Owner.Name}[{new StackTrace().GetFrame(2).GetMethod().ReflectedType.Name}]@{DateTime.Now.Second}.{DateTime.Now.Millisecond}:{origMsg}";
		}
	}
}
