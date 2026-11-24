# Almoxarifado

## Baixa consolidada das bolsas

### Entradas parciais

Em **Bolsa Entrada**, preencha **Qtde Retorno** com o total acumulado devolvido de cada item.
O botao **Entrada em estoque** considera todas as bolsas do funcionario e soma por produto
somente `retorno acumulado - quantidade ja registrada no estoque`.
Exemplo: retorno 5, entrada anterior 3, nova entrada 2. Repetir sem novos retornos nao gera entrada.
Informe origem, atendente, estado dos materiais, O.S. e endereco opcional; a data e a atual.
Cada parcial tem vinculo com sua movimentacao em `almoxarifado_apoio.tblbolsa_entradas_estoque`.
A coluna **Ja entrou no estoque** mostra o acumulado. Retornos abaixo desse valor ou acima
da quantidade enviada sao recusados. Novos incrementos permanecem permitidos.

Aplicar `scripts/20261123_entrada_bolsas.sql` **depois** do script de baixa abaixo,
com estacoes fechadas. Conceder USAGE em `almoxarifado_jac.seq_movimentacao_entrada`
e SELECT/INSERT em `almoxarifado_apoio.tblbolsa_entradas_estoque` aos perfis autorizados.
Nao conceder UPDATE/DELETE no historico. Atualizar todas as estacoes antes de usar a sequencia.
Entradas antigas feitas manualmente nao sao identificadas automaticamente: conciliar antes
de usar o novo controle. Validar permissao, concorrencia e rollback em homologacao.

### Saidas consolidadas

Em Movimentacoes > Bolsa Saida, selecione o funcionario e clique em **Baixa em estoque**.
O formulario soma por produto os itens ainda nao baixados de todas as bolsas do funcionario,
independentemente da sigla. Informe destino, atendente, O.S. e endereco (opcional).
A data e a atual; antes de finalizar, confira as quantidades consolidadas e o aviso de bloqueio.

Antes de distribuir esta versao, o administrador do banco deve executar
`scripts/20261123_baixa_bolsas.sql` no banco de cada exercicio utilizado.
O script cria as colunas de vinculo/auditoria e o trigger que protege os itens processados.
Tambem cria `almoxarifado_jac.seq_movimentacao_saida`: conceda USAGE aos perfis de saida.
Atualize todas as estacoes e feche movimentacoes antigas antes de usar a nova numeracao;
a saida normal desta versao passa a usar a mesma sequencia, evitando o antigo MAX + 1.
O script deve ser aplicado com as estacoes fora de uso.
Nao efetua baixas retroativas nem identifica saidas feitas manualmente no passado:
confira essas bolsas antes do primeiro uso para nao duplicar movimentacoes antigas.

A saida e registrada em `almoxarifado_jac.t_saida_almox`, agrupada por produto.
ACERTO ESTOQUE tambem alimenta `pcp.tbl_controle_almox_estoque`, como na saida normal.
Movimentacao e bloqueio das bolsas sao confirmados em uma transacao.
Se os itens mudarem desde a abertura do formulario, a operacao e recusada e deve ser reaberta.
Itens zerados nao geram saida, mas sao bloqueados junto com a bolsa processada.
Produtos sem cadastro/barcode valido, quantidades invalidas ou devolucoes anteriores bloqueiam a operacao.

A coluna **Baixa estoque** mostra o numero da movimentacao. Os itens baixados nao permitem
alteracao ou exclusao, nem inclusao na mesma bolsa/sigla/funcionario; outras bolsas novas
podem ser cadastradas e baixadas separadamente. Devolucoes continuam em Bolsa Entrada.
O usuario precisa das permissoes de leitura/gravacao nas tabelas envolvidas e nas sequencias
ja utilizadas pela saida normal. Nao conceder permissoes administrativas ao aplicativo.

Teste sintetico, sem gravacoes reais:
`dotnet run --project Tests/BaixaBolsa/BaixaBolsaTests.csproj -c Release`.
Antes de liberar, validar o script, rollback, duas estacoes concorrentes e uma baixa controlada em homologacao.

## Visao geral

Controle de materiais, ferramentas e movimentacoes do almoxarifado.

- Cadastro de atendentes, terceiros e bolsas de ferramentas.
- Movimentacoes de entrada e saida e acompanhamento de saldos.
- Consultas de estoque, ponto de pedido e exportacao das movimentacoes.

## Ambiente de desenvolvimento

- Windows e SDK .NET 10.
- Pacotes e licenca Telerik UI for WPF configurados localmente.
- PostgreSQL com os objetos e permissoes exigidos pelo modulo.
- PowerShell 7 e Inno Setup 6 para gerar instaladores.

Mantenha este repositorio, `BibliotecasSIG` e `Update` como pastas irmas. Os arquivos de projeto usam referencias relativas entre esses repositorios.

O projeto principal e `Almoxarifado/Almoxarifado.csproj`. Abra `Almoxarifado.sln` ou execute na raiz do repositorio:

```powershell
dotnet restore Almoxarifado.sln
dotnet build Almoxarifado.sln -c Release
```

As versoes exatas das dependencias estao no arquivo de projeto. Configure o feed Telerik sem versionar senhas ou tokens.

## Configuracao e cuidados

Confira `Almoxarifado/App.config` e a classe de configuracao de banco do modulo. Defina host, banco do exercicio e credenciais adequadas ao ambiente. A compilacao nao cria tabelas nem concede permissoes PostgreSQL.

Nao inclua senhas, tokens, arquivos pessoais ou exportacoes reais em commits. Valide consultas e gravacoes em homologacao com uma conta autorizada antes de distribuir.

## Testes e homologacao

Ha verificacoes em `Tests/MovimentacoesExport/MovimentacoesExport.csproj`:

```powershell
dotnet run --project Tests/MovimentacoesExport/MovimentacoesExport.csproj -c Release
```

Leia o teste e suas dependencias antes de executar. Confira abertura das telas, filtros, consultas e fluxos modificados. Integracoes, impressoes e gravacoes precisam de validacao especifica; um build aprovado nao garante acesso ao banco ou aos servicos externos.

## Distribuicao

`deploy.ps1`, `Setup.iss` e `scripts/` implementam a publicacao. O servidor padrao e `https://atualizasig.cipolatti.com.br`.

Mantenha `Version`, `AssemblyVersion` e `FileVersion` iguais no projeto. Para gerar os artefatos sem enviar ao servidor:

```powershell
./deploy.ps1 -Changelog @("Descricao das alteracoes") -SkipServerUpload
```

Para enviar, execute sem `-SkipServerUpload` usando credenciais autorizadas. A publicacao gera um rascunho para homologacao; a liberacao ocorre na Central. Consulte os parametros do script para caminho do Inno Setup e envio de artefatos existentes.

Push Git envia codigo/documentacao; nao equivale a publicar uma versao do aplicativo. Nao versione `bin/`, `obj/`, pacotes ou instaladores gerados.
