using Components.Implementations;
using Components.InterfaceTests;
using Xunit.Abstractions;

namespace Components.ImplementationTests;
public class VFileManagerTest ( ITestOutputHelper output ) : DFileManagerTest ( output ) {
	public override VFileManager GenerateTestObject () => new ( OwnerCore );
}