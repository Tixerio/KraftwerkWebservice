window.chartUtils = {
    transformYAxisLabels: function () {
        return function (value, index, values) {
            return (value + 50).toFixed(1);
        };
    }
};
