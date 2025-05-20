using Aspector.Core.Attributes;
using Aspector.Core.Attributes.Caching;
using Aspector.Core.Attributes.Logging;
using Aspector.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aspector.Core.Tests.Models
{
    [TestFixture]
    public class AspectAttributeSummaryTests
    {
        private MethodInfo _method1Info;
        private MethodInfo _method2Info;
        private MethodInfo _method3Info;

        [SetUp]
        public void Setup()
        {
            var fakeType = typeof(FakeClass);
            _method1Info = fakeType.GetMethod("Method1")!;
            _method2Info = fakeType.GetMethod("Method2")!;
            _method3Info = fakeType.GetMethod("Method3")!;
        }

        [Test]
        [TestCaseSource(nameof(ConstructorTestCases))]
        public void Constructor_CorrectlyChoosesBestWrapOrder(
            AspectAttribute[] method1Attributes,
            AspectAttribute[] method2Attributes,
            AspectAttribute[] method3Attributes,
            List<(Type, int)> expectedWrapOrder)
        {
            // Act
            var model = new AspectAttributeSummary([
                (_method1Info, method1Attributes),
                (_method2Info, method2Attributes),
                (_method3Info, method3Attributes)]);

            //Assert
            Assert.That(model.WrapOrder, Is.EquivalentTo(expectedWrapOrder));
        }

        public static IEnumerable<TestCaseData> ConstructorTestCases()
        {
            yield return new TestCaseData(
                new AspectAttribute[]
                {
                    new LogAttribute("Logging"),
                    new CacheResultAttribute()
                },
                new AspectAttribute[]
                {
                    new CacheResultAttribute(),
                    new LogAttribute("Log after cache")
                },
                new AspectAttribute[] {},
                new List<(Type, int)>
                {
                    (typeof(LogAttribute), 0),
                    (typeof(CacheResultAttribute), 0),
                    (typeof(LogAttribute), 1),
                });

            yield return new TestCaseData(
                new AspectAttribute[]
                {
                    new CacheResultAttribute(),
                    new LogAttribute("Log after cache")
                },
                new AspectAttribute[]
                {
                    new CacheResultAttribute(),
                    new AddLogPropertyAttribute("Constant Prop", "CONSTANT"),
                    new LogAttribute("Log after cache and add log property")
                },
                new AspectAttribute[] { },
                new List<(Type, int)>
                {
                    (typeof(LogAttribute), 0),
                    (typeof(AddLogPropertyAttribute), 0),
                    (typeof(CacheResultAttribute), 0)
                });
        }
    }

    public class FakeClass
    {
        public void Method1()
        {

        }

        public void Method2()
        {

        }

        public void Method3()
        {

        }
    }
}
