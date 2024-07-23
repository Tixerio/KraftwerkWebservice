window.chartUtils = {
    transformYAxisLabels: function () {
        return function (value, index, values) {
            return (value + 50).toFixed(1)
        }
    },
}
function resize_charts(value, key, map) {
    value.resize()
}
window.addEventListener('resize', function () {
    window.vizorECharts.charts.forEach(resize_charts)
})

