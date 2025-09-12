## K6

```ps1
# url of your prometheus remote-write compatible server
$env:K6_PROMETHEUS_RW_SERVER_URL='http://10.42.0.197:8428/api/v1/write'
$env:K6_PROMETHEUS_RW_INSECURE_SKIP_TLS_VERIFY='true'
$env:K6_PROMETHEUS_RW_TREND_STATS='p(95),p(99),min,avg,max'
$env:K6_PROMETHEUS_RW_PUSH_INTERVAL='2s'
# victoria metrics [doesn't support prometheus native histograms](https://github.com/VictoriaMetrics/VictoriaMetrics/issues/3733) but with Prometheus it might be better to use the native histogram feature
$env:K6_PROMETHEUS_RW_TREND_AS_NATIVE_HISTOGRAM='false'

# e.g. dotnet-aot:
$env:HOST='anton'
$env:PORT='8884'
$env:TAGS_NAME='dotnet-aot'
$env:TAGS_ID='12'


# or e.g. go:
$env:HOST='anton'
$env:PORT='8885'
$env:TAGS_NAME='go'
$env:TAGS_ID='22'

# then run
k6 run -o experimental-prometheus-rw benchmark.ts
```


### Formatter & Linter 

I use BiomeJS. It still quite buggy, so you can try to install it globally or use `biome.lsp.bin` setting.
Also, I disable ESLint vscode extension in the workspace.


### Grafana dashboards

[DASHBOARD](https://grafana.com/grafana/dashboards/19665-k6-prometheus/)
[DASHBOARD(NATIVE_HISTOGRAM)](https://grafana.com/grafana/dashboards/18030-k6-prometheus-native-histograms/)
