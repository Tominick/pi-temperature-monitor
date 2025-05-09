const uri = "api/measure/";
const temperatureLabel = "°C";
const minTempSuggested = 15;
const maxTempSuggested = 30;

let last24 = null;
let links = null;
let tempChart = null;
let humChart = null;
let rangeChart = null;
let rangeHumChart = null;
let myLine = null;
var sersorId = 1;

$(() => {
    var selectedId = $.urlParam('id');
    if (selectedId!=null) sersorId = parseInt(selectedId,10);
    getSensors();
    document.getElementById("defaultOpen").click();
    getData(sersorId);
});

$.urlParam = function(name){
  var results = new RegExp('[\?&]' + name + '=([^&#]*)').exec(window.location.href);
  if (results==null) {
     return null;
  }
  return decodeURI(results[1]) || 0;
}

function getSensors() {
  $.ajax({
    type: "GET",
    url: "api/sensor",
    cache: false
  })
    .done(function (data) {

      $.each(data, function (key, item) {
        if (item.sensorId == sersorId) {
          const tMt = $("#ms");
          $(tMt).text(item.description);
        }
        else {
          const tr = $("<a class=\"w3-bar-item w3-button w3-hide-small w3-padding-large w3-hover-white\" href=\"/?id="+ item.sensorId +"\" \">" + item.description+ "</a>");
          tr.appendTo($("#navBar"));
          const tr2 = $("<a class=\"w3-bar-item w3-button w3-padding-large\" href=\"/?id="+ item.sensorId +"\" \">" + item.description+ "</a>");
          tr2.appendTo($("#navDemo"));
        }
      });

      links = data;
    })
};

function getData(id) {
  $.ajax({
    type: "GET",
    url: uri + id + "/24",
    cache: false})
    .done (function(data) {
      const tLoader = $("#loader");
      $(tLoader).hide();
    
      const tLast24table = $("#last24table");
      $(tLast24table).show();
    
      const tBody = $("#todos");
      $(tBody).empty();

      const tError = $("#error");
      $(tError).empty();

      var firstDate = null;

      $.each(data, function(key, item) {
        const tr = $("<tr></tr>")
          .append($("<td></td>").text(formatDate(item.dateTime)))
          .append($("<td></td>").text(formatNumber(item.temperature)))
          .append($("<td></td>").text(formatNumber(item.humidity)));

        tr.appendTo(tBody);
        if (key==0){
          const myTitle = $("#myTitle");
          $(myTitle).text(formatNumber(item.temperature) + temperatureLabel);
          if (item.humidity != null)
          {
            $(myTitle).append(" " + formatNumber(item.humidity) + "%");
          }
          const mySubtitle = $("#mySubtitle");
          $(mySubtitle).text(formatDate(item.dateTime));
          firstDate = item.dateTime;
        }
      }
      );

      last24 = data;

      const hourContainer = $("#tabHeader");
      $(hourContainer).show();

      const inDate = $("#inDate");
      var currentDate = new Date(firstDate);
      $(inDate).val(formatDateIso(currentDate));

      const fromDate = $("#fromDate");
      $(fromDate).val(currentDate.getFullYear() + "-01-01");

      const toDate = $("#toDate");
      $(toDate).val(formatDateIso(currentDate));

      //loadChartFromDate();
    })
    .fail (function(jqXHR, textStatus, errorThrown) {
      const tLoader = $("#loader");
      $(tLoader).hide();
    
      const tLast24table = $("#last24table");
      $(tLast24table).hide();

      const tError = $("#error");
      $(tError).show();

      $(tError).empty().append("Unable to connect")
    })

};

function formatNumber(num) {
  if (num==null) return "--.-";
  return num.toFixed(1).toString().replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1,')
};

function formatDate(str) {
  return str.replace("T", " ");
};

function formatDateIso(date) {
  var d = new Date(date),
      month = '' + (d.getMonth() + 1),
      day = '' + d.getDate(),
      year = d.getFullYear();

  if (month.length < 2) month = '0' + month;
  if (day.length < 2) day = '0' + day;

  return [year, month, day].join('-');
}

function loadChartFromDate() {
  const inDate = $("#inDate");
  loadChart(sersorId, new Date($(inDate).val()));
}

function loadChart(id, date) {
    const tBar1 = $("#bar1");
    const loadButton = $("#loadDateButton");
    $(tBar1).removeAttr('style');
    $(loadButton).prop("disabled", true);
  $.ajax({
    type: "GET",
    url: uri + id + "/date/" + formatDateIso(date) + "/ByHourMinMax",
    cache: false
  })
      .done(function (data) {
      $(tBar1).hide();
      $(loadButton).prop("disabled", false);
      const tError = $("#error");
      $(tError).empty();

      var ctx = $('#tempChart');

      var lbls = $.map(data, function (x) {return x.hour});
      var lmin = $.map(data, function (x) {return x.minTemperature});
      var lmax = $.map(data, function (x) {return x.maxTemperature});

	  if (tempChart) {
        tempChart.destroy();
      }
	  
      tempChart = new Chart(ctx, {
        type: "bar",
        data: {
          labels: lbls,
          datasets: [
            {
              backgroundColor: "rgba(255, 130, 58, 0.2)",
              label: "max",
              data: lmax,
            },
            {
              label: "min",
              data: lmin,
              backgroundColor: "rgba(48, 199, 255, 0.8)",
            },
          ]
        },
          options: {
              legend: {
                  display: false
              },
					title: {
						display: true,
						text: 'Temperature '+ formatDateIso(date)
					},
					tooltips: {
						mode: 'index',
						intersect: false
					},
					responsive: true,
					scales: {
						xAxes: [{
              stacked: true,
              scaleLabel: {
                display: true,
                labelString: "Hour"
              }
						}],
						yAxes: [{
              stacked: false,
              scaleLabel: {
                display: true,
                labelString: temperatureLabel
              },
              ticks: {
                suggestedMin: minTempSuggested,
                suggestedMax: maxTempSuggested
            }
						}]
					}
				}
      });
      
    ctx = $('#humChart');
	  var hmin = $.map(data, function (x) {return x.minHumidity});
      var hmax = $.map(data, function (x) {return x.maxHumidity});
	  
	  if (humChart) {
        humChart.destroy();
    }

    if (hmin.every(x => x === null) && hmax.every(x => x === null)) {
        $(ctx).hide();
        return;
    }

    $(ctx).show();

      humChart = new Chart(ctx, {
        type: "bar",
        data: {
          labels: lbls,
          datasets: [
            {
              backgroundColor: "rgba(121, 200, 93, 0.2)",
              label: "max",
              data: hmax,
            },
            {
              label: "min",
              data: hmin,
              backgroundColor: "rgba(177, 183, 191, 1.0)",
            },
          ]
        },
          options: {
              legend: {
                  display: false
              },
					title: {
						display: true,
						text: 'Humidity '+ formatDateIso(date)
					},
					tooltips: {
						mode: 'index',
						intersect: false
					},
					responsive: true,
					scales: {
						xAxes: [{
              stacked: true,
              scaleLabel: {
                display: true,
                labelString: "Hour"
              }
						}],
						yAxes: [{
              stacked: false,
              scaleLabel: {
                display: true,
                labelString: "%"
              },
              ticks: {
                beginAtZero: true,
                min: 0,
                max: 100,
                stepSize: 20}
						}]
					}
				}
			});
      })
      .fail(function (jqXHR, textStatus, errorThrown) {
          $(tBar1).hide();
          $(loadButton).prop("disabled", false);

          const tError = $("#error");
          $(tError).show();
          $(tError).empty().append(errorThrown)
      });
}

function loadChartRange() {
  const fromDate = $("#fromDate");
  const toDate = $("#toDate");
  loadChartRangeDate(sersorId, new Date($(fromDate).val()), new Date($(toDate).val()));
}

function loadChartRangeDate(id, fromDate, toDate) {
    const tBar2 = $("#bar2");
    const loadButton = $("#loadRangeButton");
    $(tBar2).removeAttr('style');
    $(loadButton).prop("disabled", true);
    $.ajax({
        type: "GET",
        url: uri + id + "/from/" + formatDateIso(fromDate) + "/to/" + formatDateIso(toDate) + "/ByDay",
        cache: false
    })
        .done(function (data) {
            $(tBar2).hide();
            $(loadButton).prop("disabled", false);
            const tError = $("#error");
            $(tError).empty();
            var ctx = $('#rangeChart');

            var lbls = $.map(data, function (x) { return x.dateTime });
            var ltem = $.map(data, function (x) { return x.temperature });
            var htem = $.map(data, function (x) { return x.humidity });

            if (rangeChart) {
                rangeChart.destroy();
            }

            rangeChart = new Chart(ctx, {
                type: "bar",
                data: {
                    labels: lbls,
                    datasets: [
                        {
                            backgroundColor: "rgba(140, 140, 140, 0.8)",
                            borderColor: "rgba(140, 140, 140, 0.8)",
                            label: "Temperature",
                            data: ltem,
                            type: 'line',
                            pointRadius: 0,
                            fill: false,
                            lineTension: 0,
                            borderWidth: 2
                        }
                    ]
                },
                options: {
                    legend: {
                        display: false
                    },
                    title: {
                        display: true,
                        text: "Average Temperatures from " + formatDateIso(fromDate) + " to " + formatDateIso(toDate)
                    },
                    tooltips: {
                        mode: 'index',
                        intersect: false
                    },
                    responsive: true,
                    scales: {
                        xAxes: [{
                            type: 'time',
                            time: {
                                unit: 'day'
                            },
                            // distribution: 'series',
                            ticks: {
                                source: 'data',
                                autoSkip: true
                            }
                        }],
                        yAxes: [{
                            scaleLabel: {
                                display: true,
                                labelString: temperatureLabel,
                            },
                            ticks: {
                                suggestedMin: minTempSuggested,
                                suggestedMax: maxTempSuggested
                            }
                        }]
                    },
                    tooltips: {
                        intersect: false,
                        mode: 'index',
                        callbacks: {
                            label: function (tooltipItem, myData) {
                                var label = myData.datasets[tooltipItem.datasetIndex].label || '';
                                if (label) {
                                    label += ': ';
                                }
                                label += parseFloat(tooltipItem.value).toFixed(2);
                                return label;
                            }
                        }
                    }
                }
            });

            ctx = $('#rangehumChart');

            if (rangeHumChart) {
                rangeHumChart.destroy();
            }

            if (htem.every(x => x === null)) {
                $(ctx).hide();
                return;
            }
            $(ctx).show();

            rangeHumChart = new Chart(ctx, {
                type: "bar",
                data: {
                    labels: lbls,
                    datasets: [
                        {
                            backgroundColor: "rgba(255, 130, 58, 0.2)",
                            borderColor: "rgba(255, 130, 58, 0.2)",
                            label: "Humidity",
                            data: htem
                        }
                    ]
                },
                options: {
                    legend: {
                        display: false
                    },
                    title: {
                        display: true,
                        text: "Average Humidity from " + formatDateIso(fromDate) + " to " + formatDateIso(toDate)
                    },
                    tooltips: {
                        mode: 'index',
                        intersect: false
                    },
                    responsive: true,
                    scales: {
                        xAxes: [{
                            type: 'time',
                            time: {
                                unit: 'day'
                            },
                            // distribution: 'series',
                            ticks: {
                                source: 'data',
                                autoSkip: true
                            }
                        }],
                        yAxes: [{
                            scaleLabel: {
                                display: true,
                                labelString: '%',
                            },
                            ticks: {
                                beginAtZero: true,
                                min: 0,
                                max: 100,
                                stepSize: 20,
                            }
                        }]
                    },
                    tooltips: {
                        intersect: false,
                        mode: 'index',
                        callbacks: {
                            label: function (tooltipItem, myData) {
                                var label = myData.datasets[tooltipItem.datasetIndex].label || '';
                                if (label) {
                                    label += ': ';
                                }
                                label += parseFloat(tooltipItem.value).toFixed(2);
                                return label;
                            }
                        }
                    }
                }
            });
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
            $(tBar2).hide();
            $(loadButton).prop("disabled", false);

            const tError = $("#error");
            $(tError).show();
            $(tError).empty().append(errorThrown)
        });
  }
