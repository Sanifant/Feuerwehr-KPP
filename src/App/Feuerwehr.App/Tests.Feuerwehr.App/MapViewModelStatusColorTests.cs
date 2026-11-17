using System.Reflection;
using de.openelp.feuerwehr.App.ViewModels;
using de.openelp.feuerwehr.Common.Models;
using Mapsui.Styles;

namespace de.openelp.feuerwehr.Tests.App
{
    public class MapViewModelStatusColorTests
    {
        [Theory]
        [InlineData(HydrantStatus.Operational, 37, 99, 235)]
        [InlineData(HydrantStatus.Defective, 255, 0, 0)]
        [InlineData(HydrantStatus.Blocked, 255, 0, 0)]
        [InlineData(HydrantStatus.UnderMaintenance, 255, 144, 0)]
        public void GetStatusColor_ReturnsExpectedColorForEachHydrantStatus(HydrantStatus status, byte red, byte green, byte blue)
        {
            Color color = InvokeGetStatusColor(status);

            Assert.Equal((byte)255, color.A);
            Assert.Equal(red, color.R);
            Assert.Equal(green, color.G);
            Assert.Equal(blue, color.B);
        }

        private static Color InvokeGetStatusColor(HydrantStatus status)
        {
            MethodInfo method = typeof(MapViewModel).GetMethod("GetStatusColor", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(typeof(MapViewModel).FullName, "GetStatusColor");

            object? result = method.Invoke(null, new object[] { status });
            return Assert.IsType<Color>(result);
        }
    }
}
