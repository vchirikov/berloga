import { check } from 'k6';
import { test } from 'k6/execution';
import http, { type RefinedResponse } from 'k6/http';

import type { Options } from 'k6/options';

const host = __ENV.HOST;
if (!host) {
  test.abort('Provide "HOST" env variable');
}

const port = __ENV.PORT;
if (!port) {
  test.abort('Provide "PORT" env variable');
}

const tags = {
  id: __ENV.TAGS_ID,
  name: __ENV.TAGS_NAME,
};

if (!tags.id) {
  test.abort('Provide "TAGS_ID" env variable');
}
if (!tags.name) {
  test.abort('Provide "TAGS_NAME" env variable');
}

export const options: Options = {
  tags: {
    testid: `${tags.name}-${tags.id}`,
  },
  scenarios: {
    constant: {
      executor: 'constant-vus',
      duration: '3m',
      vus: 50,
    },
  },
};

const headers = { headers: { 'Content-Type': 'application/json' } } as const;
const id = Number.parseInt(tags.id, 10);
const json = JSON.stringify({ value: id });
const url = `http://${host}:${port}/${tags.id}`;
const expected = (id * 2).toString();

const main = () => {
  const res: RefinedResponse<'text'> = http.post(url, json, headers);

  check(res, {
    'is status 200': r => r.status === 200,
    'is valid response': r => r.body === expected,
  });
};

export default main;
