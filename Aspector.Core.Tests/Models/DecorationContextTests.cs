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

        private class ExampleDecoratedClass
        {
            public void ExampleMethod(int aNumber, string aString, DateTime aDate, CancellationToken aCancellationToken) { }

            public void ExampleMethod_NoParams() { }
        }
    }
}
