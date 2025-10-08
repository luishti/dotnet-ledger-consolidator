import http from 'k6/http';
import { check, sleep } from 'k6';

// Este script envia requisições de escrita e leitura para avaliar o desempenho
// dos serviços. A carga é de ~50 requisições por segundo e o threshold de
// falhas deve ser menor ou igual a 5 %, conforme o requisito de negócio【926386920860220†L83-L87】.

export const options = {
  stages: [
    { duration: '5s', target: 50 },  // rampa até 50 VUs
    { duration: '20s', target: 50 }, // sustenta 50 VUs
    { duration: '5s', target: 0 },   // desaceleração
  ],
  thresholds: {
    http_req_failed: ['rate<=0.05'], // taxa de falha ≤ 5 %
    http_req_duration: ['p(95)<750'], // 95 % das requisições devem responder em <750 ms
  },
};

export default function () {
  const payload = JSON.stringify({
    merchantId: 'loadtest',
    amount: Math.random() * 100,
    type: Math.random() > 0.5 ? 'Credit' : 'Debit',
  });

  // Cria um lançamento
  const postRes = http.post('http://localhost:5000/entries', payload, {
    headers: { 'Content-Type': 'application/json' },
  });
  check(postRes, { 'post status is 200 or 201': (r) => r.status === 200 || r.status === 201 });

  // Consulta saldo diário (usando a data atual)
  const today = new Date().toISOString().substring(0, 10);
  const getRes = http.get(`http://localhost:5001/daily-balances?date=${today}`);
  check(getRes, { 'get status is 200': (r) => r.status === 200 });

  sleep(1);
}