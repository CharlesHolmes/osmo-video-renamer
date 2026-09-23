using Cocona.Builder;
using OsmoVideoRenamer.Configuration;
using OsmoVideoRenamer.ParameterLogging;

namespace OsmoVideoRenamer.UnitTests.Configuration
{
    [TestClass]
    public class FilterConfigurationTests
    {
        [TestMethod]
        public void FilterConfiguration_ShouldRegisterParameterLoggingFilter()
        {
            var mockedApp = new Mock<ICoconaCommandsBuilder>();
            var mockedPropertyDict = new Mock<IDictionary<string, object?>>();
            var mockedObjectList = new Mock<IList<object>>();
            mockedPropertyDict.Setup(m => m.TryGetValue("Cocona.Builder.CoconaCommandsBuilder+Filters", out It.Ref<object>.IsAny!))
                .Callback((string key, out object result) => { result = mockedObjectList.Object; })
                .Returns(true);
            mockedApp.Setup(m => m.Properties).Returns(mockedPropertyDict.Object);

            FilterConfiguration.RegisterAllFilters(mockedApp.Object);

            mockedObjectList.Verify(m => m.Add(It.IsAny<ParameterLoggingCommandFilterFactory>()));
        }
    }
}
