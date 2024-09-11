using Components.Implementations;
using Components.Interfaces;
using Components.Library;

namespace InputResender.WindowsGUI {
	public interface IProcessorCreator {
		public string CommonName { get; }
		public bool ShowGUI ();
		public DInputProcessor GetNewProcessor ( CoreBase targetCore );
	}

	public class PassthroughProcessorCreator : IProcessorCreator {
		public string CommonName => "Keyboard Passthrough";
		public DInputProcessor GetNewProcessor ( CoreBase targetCore ) => new VInputProcessor ( targetCore );
		public bool ShowGUI () => true;
	}
}