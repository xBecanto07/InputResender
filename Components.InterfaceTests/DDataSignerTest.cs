using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System;
using Xunit;

namespace Components.InterfaceTests {
	public abstract class DDataSignerTest : ComponentTestBase<DDataSigner> {
		protected const int DefaultDataSize = 64;
		public override CoreBase CreateCoreBase () => new CoreBaseMock ();

		[Fact]
		public void FullProcess () {
			TestObject.Key = GenIV ( 42 );
			byte[] IV = GenIV ( 1 );
			byte[] data = GenData ( DefaultDataSize );

			byte[] coded = TestObject.Encrypt ( data, IV );
			coded.Should ().NotBeNull ().And.HaveCountGreaterThanOrEqualTo ( DefaultDataSize );
			TestObject.TestIntegrity ( coded ).Should ().BeTrue ();
			TestObject.TestPsswd ( coded, IV ).Should ().BeTrue ();
			TestObject.Decrypt ( coded, IV )
				.Should ().NotBeNull ().
				And.Equal ( data ).
				And.NotBeSameAs ( data );
		}

		[Fact]
		public void IVChangesCipher () {
			const int reps = 4;
			TestObject.Key = GenIV ( 42 );
			byte[][] IVs = new byte[reps][];
			byte[][] ciphers = new byte[reps][];
			byte[] data = GenData ( DefaultDataSize );

			for (int i = 0; i < reps; i++ ) {
				IVs[i] = TestObject.GenerateIV ( BitConverter.GetBytes ( i ) );
				ciphers[i] = TestObject.Encrypt ( data, IVs[i] );
			}
			IVs.Should ().OnlyHaveUniqueItems ();
			ciphers.Should ().OnlyHaveUniqueItems ();
		}

		[Fact]
		public void GenerateIVIsRandom () {
			const int reps = 4;
			byte[][] IVs = new byte[reps][];
			for ( int i = 0; i < reps; i++ ) IVs[i] = TestObject.GenerateIV ();
			IVs.Should ().OnlyHaveUniqueItems ();
		}

		[Fact]
		public void CheckIntegrityDetectsError () {
			TestObject.Key = GenIV ( 42 );
			byte[] IV = GenIV ( 1 );
			byte[] data = GenData ( DefaultDataSize );

			byte[] coded = TestObject.Encrypt ( data, IV );
			coded[TestObject.ChecksumSize + 1] += 1;
			TestObject.TestIntegrity ( coded ).Should ().BeFalse ();
		}

		[Fact]
		public void CheckPsswdDetectWrongOne () {
			byte[] Key = TestObject.Key = GenIV ( 42 );
			byte[] IV = GenIV ( 1 );
			byte[] data = GenData ( DefaultDataSize );

			byte[] coded = TestObject.Encrypt ( data, IV );
			TestObject.TestPsswd ( coded, IV ).Should ().BeTrue ();
			TestObject.TestPsswd ( coded, Key ).Should ().BeFalse ();
			TestObject.Key = IV;
			TestObject.TestPsswd ( coded, IV ).Should ().BeFalse ();
			TestObject.TestPsswd ( coded, Key ).Should ().BeFalse ();
			TestObject.TestPsswd ( coded, null ).Should ().BeFalse ();
		}

		protected byte[] GenData (int length) {
			byte[] ret = new byte[length];
			for ( int i = 0; i < length; i++ ) ret[i] = (byte)i;
			return ret;
		}

		protected byte[] GenIV (int val) => TestObject.GenerateIV ( BitConverter.GetBytes ( val.GetHashCode () ) );
	}

	public class MDataSignerTest : DDataSignerTest {
		public override DDataSigner GenerateTestObject () => new MDataSigner ( OwnerCore );
	}
}