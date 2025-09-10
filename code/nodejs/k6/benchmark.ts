import { check } from 'k6';
import http from 'k6/http';

import type { Options } from 'k6/options';

export const options: Options = {
  vus: 20,
  duration: '60s',
};

const headers = { headers: { 'Content-Type': 'application/json' } } as const;
const json = JSON.stringify({ value: 123 });

const main = () => {
  const res = http.post(`http://${__ENV.HOST}:${__ENV.PORT}/1`, json, headers);

  check(res, {
    'is status 200': r => r.status === 200,
    'is valid response': r => r.body === '124',
  });
};

export default main;
