# Visão do produto

## Declaração de visão

Uma plataforma privada onde cada pessoa controla seus gastos, acompanha seus investimentos e planeja suas metas num só lugar, acessível de qualquer dispositivo, principalmente do celular.

## Público

Grupo pequeno e conhecido: o autor (que também é o administrador), a esposa e amigos. Não há planos de abrir a plataforma ao público, cobrar assinatura ou escalar para muitos usuários.

Consequências dessa escolha:

- Não é preciso se preocupar com escala, cobrança ou marketing.
- Ainda assim, são dados financeiros de terceiros. Segurança, isolamento entre usuários e backups são requisitos desde o primeiro dia.

## Objetivos, em ordem de prioridade

1. **Controle de gastos e orçamento:** registrar receitas e despesas de forma rápida, acompanhar o orçamento mensal por categoria.
2. **Acompanhamento de investimentos:** carteira, aportes, resgates, cotações e rentabilidade.
3. **Metas e planejamento:** metas com valor e prazo, vinculadas aos investimentos, com projeção baseada no histórico real.

A ordem é intencional: os gastos geram os dados que as metas usam (quanto sobra por mês).

## Princípios do produto

- **Mobile-first.** A maioria dos lançamentos acontece no celular, na hora da compra. Lançar uma despesa deve levar segundos.
- **Entrada de dados sem atrito.** Lançamento manual rápido primeiro, chat com IA em seguida, Open Finance no futuro.
- **Privacidade por padrão.** Cada usuário vê apenas os próprios dados financeiros, inclusive em relação ao administrador. A única exceção é o console de consultas do root, uma conta de sistema única que consulta qualquer tabela do banco; cada consulta fica registrada na auditoria.
- **Nada acontece em silêncio.** Qualquer dado gerado automaticamente (pela IA ou por integrações) é confirmado ou rastreável.

## Fora do escopo (por enquanto)

- Compartilhamento de finanças entre usuários (contas conjuntas, espaços familiares).
- Aplicativo nativo para iOS ou Android. O acesso móvel é via PWA.
- Uso comercial ou multi-empresa.
- Recomendações de investimento.
