/**
 * ApexCharts Configuration - Sistema Voto Electronico
 * Configuracion global y helpers para graficos
 */

(function () {
  'use strict';

  /**
   * Obtiene los colores del tema actual
   */
  function getThemeColors() {
    const style = getComputedStyle(document.documentElement);
    return {
      accent: style.getPropertyValue('--accent').trim(),
      success: style.getPropertyValue('--color-success').trim(),
      warning: style.getPropertyValue('--color-warning').trim(),
      error: style.getPropertyValue('--color-error').trim(),
      info: style.getPropertyValue('--color-info').trim(),
      purple: style.getPropertyValue('--color-purple').trim(),
      textPrimary: style.getPropertyValue('--text-primary').trim(),
      textSecondary: style.getPropertyValue('--text-secondary').trim(),
      textMuted: style.getPropertyValue('--text-muted').trim(),
      borderColor: style.getPropertyValue('--border-color').trim(),
      bgPanel: style.getPropertyValue('--bg-panel').trim()
    };
  }

  /**
   * Obtiene el modo del tema actual
   */
  function getThemeMode() {
    return document.documentElement.getAttribute('data-theme') || 'light';
  }

  /**
   * Opciones base para todos los graficos
   */
  function getBaseOptions() {
    const colors = getThemeColors();
    const mode = getThemeMode();

    return {
      chart: {
        background: 'transparent',
        fontFamily: 'Inter, sans-serif',
        toolbar: {
          show: false
        },
        animations: {
          enabled: true,
          easing: 'easeinout',
          speed: 800
        }
      },
      theme: {
        mode: mode
      },
      colors: [colors.accent, colors.success, colors.info, colors.warning, colors.purple],
      grid: {
        borderColor: colors.borderColor,
        strokeDashArray: 4
      },
      xaxis: {
        labels: {
          style: {
            colors: colors.textMuted,
            fontSize: '12px'
          }
        },
        axisBorder: {
          color: colors.borderColor
        },
        axisTicks: {
          color: colors.borderColor
        }
      },
      yaxis: {
        labels: {
          style: {
            colors: colors.textMuted,
            fontSize: '12px'
          }
        }
      },
      legend: {
        labels: {
          colors: colors.textSecondary
        }
      },
      tooltip: {
        theme: mode
      }
    };
  }

  /**
   * Crea un grafico de barras horizontales para resultados electorales
   */
  function createResultsBarChart(elementId, data) {
    const colors = getThemeColors();
    const baseOptions = getBaseOptions();

    const options = {
      ...baseOptions,
      series: [{
        name: 'Votos',
        data: data.map(d => d.votos)
      }],
      chart: {
        ...baseOptions.chart,
        type: 'bar',
        height: Math.max(300, data.length * 50)
      },
      plotOptions: {
        bar: {
          horizontal: true,
          borderRadius: 6,
          distributed: true,
          dataLabels: {
            position: 'top'
          }
        }
      },
      dataLabels: {
        enabled: true,
        formatter: function (val, opt) {
          const percentage = data[opt.dataPointIndex].porcentaje;
          return val + ' (' + percentage.toFixed(1) + '%)';
        },
        style: {
          fontSize: '12px',
          colors: [colors.textPrimary]
        },
        offsetX: 30
      },
      xaxis: {
        categories: data.map(d => d.nombre),
        labels: {
          style: {
            colors: colors.textMuted
          }
        }
      }
    };

    const chart = new ApexCharts(document.querySelector(elementId), options);
    chart.render();

    return chart;
  }

  /**
   * Crea un grafico de pie/donut para distribucion de votos
   */
  function createDonutChart(elementId, data, options = {}) {
    const colors = getThemeColors();
    const baseOptions = getBaseOptions();

    const chartOptions = {
      ...baseOptions,
      series: data.map(d => d.votos),
      labels: data.map(d => d.nombre),
      chart: {
        ...baseOptions.chart,
        type: 'donut',
        height: options.height || 350
      },
      plotOptions: {
        pie: {
          donut: {
            size: '60%',
            labels: {
              show: true,
              total: {
                show: true,
                label: 'Total Votos',
                color: colors.textPrimary,
                formatter: function (w) {
                  return w.globals.seriesTotals.reduce((a, b) => a + b, 0);
                }
              }
            }
          }
        }
      },
      legend: {
        position: 'bottom',
        labels: {
          colors: colors.textSecondary
        }
      }
    };

    const chart = new ApexCharts(document.querySelector(elementId), chartOptions);
    chart.render();

    return chart;
  }

  /**
   * Crea un grafico de area para tendencias
   */
  function createAreaChart(elementId, series, categories) {
    const colors = getThemeColors();
    const baseOptions = getBaseOptions();

    const options = {
      ...baseOptions,
      series: series,
      chart: {
        ...baseOptions.chart,
        type: 'area',
        height: 300
      },
      stroke: {
        curve: 'smooth',
        width: 2
      },
      fill: {
        type: 'gradient',
        gradient: {
          shadeIntensity: 1,
          opacityFrom: 0.4,
          opacityTo: 0.1
        }
      },
      xaxis: {
        ...baseOptions.xaxis,
        categories: categories
      }
    };

    const chart = new ApexCharts(document.querySelector(elementId), options);
    chart.render();

    return chart;
  }

  /**
   * Actualiza un grafico cuando cambia el tema
   */
  function updateChartTheme(chart) {
    const mode = getThemeMode();
    const colors = getThemeColors();

    chart.updateOptions({
      theme: { mode: mode },
      grid: { borderColor: colors.borderColor },
      xaxis: {
        labels: { style: { colors: colors.textMuted } }
      },
      yaxis: {
        labels: { style: { colors: colors.textMuted } }
      },
      legend: {
        labels: { colors: colors.textSecondary }
      },
      tooltip: { theme: mode }
    });
  }

  // Exponer API global
  window.VotoCharts = {
    getThemeColors: getThemeColors,
    getBaseOptions: getBaseOptions,
    createResultsBarChart: createResultsBarChart,
    createDonutChart: createDonutChart,
    createAreaChart: createAreaChart,
    updateChartTheme: updateChartTheme
  };

  // Auto-actualizar graficos cuando cambie el tema
  window.addEventListener('themechange', function () {
    // Los graficos individuales deben registrarse para actualizacion
    console.log('Tema cambiado, actualizar graficos manualmente');
  });
})();
