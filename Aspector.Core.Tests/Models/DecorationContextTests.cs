using Aspector.Core.Models;

namespace Aspector.Core.Tests.Models
{
    [TestFixture]
    public class DecorationContextTests
    {
        private DecorationContext _sut;
        private CancellationToken _globalToken;

        [SetUp]
        public void Setup()
        {
            var targetType = typeof(ExampleDecoratedClass);
            var targetMethod = targetType.GetMethod("ExampleMethod");
            var parameters = targetMethod!.GetParameters();
            _globalToken = new CancellationToken();

            _sut = new DecorationContext(parameters, targetMethod, targetType, _globalToken);
        }

        [Test]
        public void GetParameterByName_WhenNoParameters_ThrowsKeyNotFound()
        {
            // Arrange
            var targetType = typeof(ExampleDecoratedClass);
            var targetMethod = targetType.GetMethod("ExampleMethod_NoParams");
            var parameters = targetMethod!.GetParameters();

            var sut = new DecorationContext(parameters, targetMethod, targetType, _globalToken);

            var expectedErrorMessage = "Parameter myParam could not be found.  No parameters required for ExampleMethod_NoParams";

            // Act / Assert
            var actualException = Assert.Throws<KeyNotFoundException>(() => sut.GetParameterByName("myParam", []));

            Assert.That(actualException.Message, Is.EqualTo(expectedErrorMessage));
        }

        [Test]
        public void GetParameterByName_WhenNamedParamNotPresent_ThrowsKeyNotFound()
        {
            // Arrange
            var requestedParam = "myParam";

            var expectedErrorMessage = "Parameter myParam could not be found in method parameters for ExampleMethod";

            // Act / Assert
            var actualException = Assert.Throws<KeyNotFoundException>(() => _sut.GetParameterByName(requestedParam, [ 1, "1", DateTime.UtcNow, CancellationToken.None ]));

            Assert.That(actualException.Message, Is.EqualTo(expectedErrorMessage));
        }

        [Test]
        public void GetParameterByName_WhenNamedParamNotPresentForType_ThrowsKeyNotFound()
        {
            // Arrange
            var requestedParam = "aDate";

            var expectedErrorMessage = "Parameter aDate, with type of CancellationToken could not be found in method parameters for ExampleMethod";

            // Act / Assert
            var actualException = Assert.Throws<KeyNotFoundException>(() => _sut.GetParameterByName<CancellationToken>(requestedParam, [1, "1", DateTime.UtcNow, CancellationToken.None]));

            Assert.That(actualException.Message, Is.EqualTo(expectedErrorMessage));
        }

        [TestCase("aNumber", typeof(int))]
        [TestCase("aString", typeof(string))]
        [TestCase("aDate", typeof(DateTime))]
        [TestCase("aCancellationToken", typeof(CancellationToken))]
        public void GetParameterByName_WhenNameParamPresent_ReturnsCorrectParameterValueFromInputArray(string name, Type expectedType)
        {
            // Act / Assert
            var result = _sut.GetParameterByName(name, [1, "1", DateTime.UtcNow, CancellationToken.None]);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.GetType(), Is.EqualTo(expectedType));
        }

        [TestCase(true, -1, 0)]
        [TestCase(false, 0, 10)]
        public void TryGetFirstOrDefault_WhenMultipleParamsOfSameType_BehavesAccordingToInputFlag(
            bool returnDefaultForMultiple,
            int expectedIndex,
            int expectedResult)
        {
            // Arrange
            var targetType = typeof(ExampleDecoratedClass);
            var targetMethod = targetType.GetMethod("ExampleMethod_MultipleInts");
            var parameters = targetMethod!.GetParameters();

            var sut = new DecorationContext(parameters, targetMethod, targetType, _globalToken);

            // Act
            var foundIndex = sut.TryGetFirstOrDefault<int>([10, 20], out var result, returnDefaultForMultiple);

            // Assert
            Assert.That(foundIndex, Is.EqualTo(expectedIndex));
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [TestCase(true, 11, 10, false)]
        [TestCase(false, 12, 12, true)]
        public void TrySetFirst_WhenMultipleParamsOfSameType_BehavesAccordingToInputFlag(
            bool failIfMultiple,
            int replacementValue,
            int expectedFirstValue,
            bool expectedSuccess)
        {
            // Arrange
            var targetType = typeof(ExampleDecoratedClass);
            var targetMethod = targetType.GetMethod("ExampleMethod_MultipleInts");
            var parameters = targetMethod!.GetParameters();

            var sut = new DecorationContext(parameters, targetMethod, targetType, _globalToken);
            var inputParameters = new object?[] { 10, 20 };

            // Act
            var success = sut.TrySetFirst<int>(inputParameters, replacementValue, failIfMultiple);

            // Assert
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(inputParameters[0], Is.EqualTo(expectedFirstValue));
        }

        private class ExampleDecoratedClass
        {
            public void ExampleMethod(int aNumber, string aString, DateTime aDate, CancellationToken aCancellationToken) { }

            public void ExampleMethod_NoParams() { }

            public void ExampleMethod_MultipleInts(int aNumber, int anotherNumber) { }
        }
    }
}
