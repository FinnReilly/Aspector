using Aspector.Core.Attributes;
using Aspector.Core.Attributes.Caching;
using Aspector.Core.Attributes.Logging;
using Aspector.Core.Models;
using Aspector.Core.Models.Registration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Aspector.Core.Tests.Models.Registration
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

            // Assert
            Assert.That(model.WrapOrderFromInnermost, Is.EquivalentTo(expectedWrapOrder));

            foreach (var layer in model.LayersFromInnermostByMethod)
            {
                var wrapOrderIndicesFromLayers = layer.Value.Select(l => model.WrapOrderFromInnermost.FindIndex(wrapEntry => wrapEntry.AspectType == l.AspectType && wrapEntry.LayerIndex == l.LayerIndex));
                var wrapOrders_ordered = wrapOrderIndicesFromLayers.Order();

                Assert.That(wrapOrderIndicesFromLayers, Is.EquivalentTo(wrapOrders_ordered));
            }
        }

        [Test]
        [TestCaseSource(nameof(ConstructorTestCases_Permissive))]
        public void Constructor_ChoosesWrapOrderWhichAlignsWithOrderingOfAttributeLayers(
            AspectAttribute[] method1Attributes,
            AspectAttribute[] method2Attributes,
            AspectAttribute[] method3Attributes)
        {
            // Act
            var model = new AspectAttributeSummary([
                (_method1Info, method1Attributes),
                (_method2Info, method2Attributes),
                (_method3Info, method3Attributes)]);

            // Assert
            foreach (var layer in model.LayersFromInnermostByMethod)
            {
                var wrapOrderIndicesFromLayers = layer.Value.Select(l => model.WrapOrderFromInnermost.FindIndex(wrapEntry => wrapEntry.AspectType == l.AspectType && wrapEntry.LayerIndex == l.LayerIndex));
                var wrapOrders_ordered = wrapOrderIndicesFromLayers.Order();

                Assert.That(wrapOrderIndicesFromLayers, Is.EquivalentTo(wrapOrders_ordered));
            }
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

            yield return new TestCaseData(
                new AspectAttribute[]
                {
                    new CacheResultAttribute(),
                    new LogAttribute("log after cache")
                },
                new AspectAttribute[]
                {
                    new LogAttribute("log before cache"),
                    new CacheResultAttribute()
                },
                new AspectAttribute[]
                {
                    new LogAttribute("log before all"),
                    new CacheResultAttribute(),
                    new AddLogPropertyAttribute("ContextKey") { ConstantValue = 0 },
                },
                new List<(Type, int)>
                {
                    (typeof(AddLogPropertyAttribute), 0),
                    (typeof(CacheResultAttribute), 0),
                    (typeof(LogAttribute), 0),
                    (typeof(CacheResultAttribute), 1)
                });

            yield return new TestCaseData(
                new AspectAttribute[]
                {
                    new AddLogPropertyAttribute("Method") { ConstantValue = 0 },
                    new CacheResultAttribute()
                },
                new AspectAttribute[]
                {
                    new AddLogPropertyAttribute("Method") { ConstantValue = 1 },
                    new CacheResultAttribute(),
                    new LogAttribute("Some logging or other", LogLevel.Debug)
                },
                new AspectAttribute[]
                {
                    new AddLogPropertyAttribute("Method") { ConstantValue = 2 },
                    new CacheResultAttribute(),
                },
                new List<(Type, int)>
                {
                    (typeof(LogAttribute), 0),
                    (typeof(CacheResultAttribute), 0),
                    (typeof(AddLogPropertyAttribute), 0)
                });

            //yield return new TestCaseData(
            //    new AspectAttribute[]
            //    {
            //        new LogAttribute("Outermost"),
            //        new CacheResultAttribute(),
            //        new LogAttribute("Log something else"),
            //        new LogAttribute("Log another thing just to test aggregation"),
            //        new CacheResultAttribute(),
            //        new LogAttribute("Log something"),
            //        new AddLogPropertyAttribute("Innermost"),
            //    },
            //    new AspectAttribute[]
            //    {
            //        new LogAttribute("Outermost"),
            //        new CacheResultAttribute(),
            //        new LogAttribute("More logging"),
            //        new CacheResultAttribute(),
            //        new LogAttribute("Should also be layer 0"),
            //        new LogAttribute("Should be layer 0")
            //    },
            //    new AspectAttribute[]
            //    {
            //        new LogAttribute("Outermost"),
            //        new CacheResultAttribute(),
            //        new LogAttribute("This is layer 1"),
            //        new CacheResultAttribute(),
            //    },
            //    new List<(Type, int)>
            //    {
            //        (typeof(AddLogPropertyAttribute), 0),
            //        (typeof(LogAttribute), 0),
            //        (typeof(CacheResultAttribute), 0),
            //        (typeof(LogAttribute), 1),
            //        (typeof(CacheResultAttribute), 1),
            //        (typeof(LogAttribute), 2)
            //    });
        }

        public static IEnumerable<TestCaseData> ConstructorTestCases_Permissive()
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
                new AspectAttribute[] { });

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
                new AspectAttribute[] { });

            yield return new TestCaseData(
                new AspectAttribute[]
                {
                    new CacheResultAttribute(),
                    new LogAttribute("log after cache")
                },
                new AspectAttribute[]
                {
                    new LogAttribute("log before cache"),
                    new CacheResultAttribute()
                },
                new AspectAttribute[]
                {
                    new LogAttribute("log before all"),
                    new CacheResultAttribute(),
                    new AddLogPropertyAttribute("ContextKey") { ConstantValue = 0 }
                });

            yield return new TestCaseData(
                new AspectAttribute[]
                {
                    new AddLogPropertyAttribute("Method") { ConstantValue = 0 },
                    new CacheResultAttribute()
                },
                new AspectAttribute[]
                {
                    new AddLogPropertyAttribute("Method") { ConstantValue = 1 },
                    new CacheResultAttribute(),
                    new LogAttribute("Some logging or other", LogLevel.Debug)
                },
                new AspectAttribute[]
                {
                    new AddLogPropertyAttribute("Method") { ConstantValue = 2 },
                    new CacheResultAttribute(),
                });

            yield return new TestCaseData(
                new AspectAttribute[]
                {
                    new LogAttribute("Outermost"),
                    new CacheResultAttribute(),
                    new LogAttribute("Log something else"),
                    new LogAttribute("Log another thing just to test aggregation"),
                    new CacheResultAttribute(),
                    new LogAttribute("Log something"),
                    new AddLogPropertyAttribute("Innermost"),
                },
                new AspectAttribute[]
                {
                    new LogAttribute("Outermost"),
                    new CacheResultAttribute(),
                    new LogAttribute("More logging"),
                    new CacheResultAttribute(),
                    new LogAttribute("Should also be layer 0"),
                    new LogAttribute("Should be layer 0")
                },
                new AspectAttribute[]
                {
                    new LogAttribute("Outermost"),
                    new CacheResultAttribute(),
                    new LogAttribute("This is layer 1"),
                    new CacheResultAttribute(),
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
