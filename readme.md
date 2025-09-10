## K6

```bash
# where victoria-metrics-headless.infra.svc.cluster.local:8428 is your prometheus remote-write compatible server
# I use victoria metrics
export K6_PROMETHEUS_RW_SERVER_URL=http://victoria-metrics-headless.infra.svc.cluster.local:8428/api/v1/write \
export K6_PROMETHEUS_RW_INSECURE_SKIP_TLS_VERIFY=true
export K6_PROMETHEUS_RW_TREND_STATS="p(95),p(99),min,avg,max"
export K6_PROMETHEUS_RW_PUSH_INTERVAL=1s
# victoria metrics [doesn't support prometheus native histograms](https://github.com/VictoriaMetrics/VictoriaMetrics/issues/3733) but with Prometheus it might be better to use the native histogram feature
export K6_PROMETHEUS_RW_TREND_AS_NATIVE_HISTOGRAM=false

k6 run --tag NAME=VALUE -o experimental-prometheus-rw script.js
```

### Formatter & Linter 

I use BiomeJS. It still quite buggy, so you can try to install it globally or use `biome.lsp.bin` setting.
Also, I disable ESLint vscode extension in the workspace.


### Grafana dashboards

[DASHBOARD](https://grafana.com/grafana/dashboards/19665-k6-prometheus/)
[DASHBOARD(NATIVE_HISTOGRAM)](https://grafana.com/grafana/dashboards/18030-k6-prometheus-native-histograms/)
