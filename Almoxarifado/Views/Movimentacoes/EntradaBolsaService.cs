using Almoxarifado.DataBase;
using Dapper;
using Npgsql;

namespace Almoxarifado.Views.Movimentacoes;

internal sealed class EntradaBolsaItem
{
    public int Id { get; set; }
    public int? Produto { get; set; }
    public decimal? Enviado { get; set; }
    public decimal Retornado { get; set; }
    public decimal Registrado { get; set; }
    public decimal Pendente => Retornado - Registrado;
    public string Versao { get; set; } = "";
    public string? Descricao { get; set; }
    public string? Unidade { get; set; }
    public string? Barcode { get; set; }
    public int Barcodes { get; set; }
}
internal sealed record EntradaBolsaProduto(int Produto,string Descricao,decimal Quantidade);

internal sealed class EntradaBolsaService
{
    internal static readonly string[] Origens = ["RETORNO INTERNO", "PRODUÇÃO PEÇA NOVA", "CLIENTE",
        "ACERTO ESTOQUE", "ACERTO MOMADE", "TRANSFERENCIA ALMOX", "COMPRA"];
    internal static readonly string[] Estados = ["BOM", "MÉDIA", "RUIM"];
    private readonly string conexao = DataBaseSettings.Instance.ConnectionString!;
    private readonly string usuario = DataBaseSettings.Instance.Username ?? "";
    private const string Sql = """
        SELECT b.cod_controlelinha AS Id,b.codcompladicional AS Produto,b.quantidade::numeric AS Enviado,
            COALESCE(b.quantidade_retorno,0)::numeric AS Retornado,b.entrada_estoque_qtde AS Registrado,
            b.xmin::text AS Versao,p.descricao_completa AS Descricao,p.unidade AS Unidade,
            (SELECT MIN(barcode) FROM producao.tbl_barcodes WHERE codigo=b.codcompladicional) AS Barcode,
            (SELECT COUNT(DISTINCT barcode)::integer FROM producao.tbl_barcodes WHERE codigo=b.codcompladicional) AS Barcodes
        FROM almoxarifado_apoio.tblsaidabolsa b
        LEFT JOIN producao.qry3descricoes p ON p.codcompladicional=b.codcompladicional
        WHERE b.codfun=@funcionario
        ORDER BY b.cod_controlelinha
        """;
    internal async Task<List<EntradaBolsaItem>> Carregar(int funcionario)
    {
        await using var db=new NpgsqlConnection(conexao);
        try { return (await db.QueryAsync<EntradaBolsaItem>(Sql,new {funcionario})).ToList(); }
        catch(PostgresException e) when(e.SqlState=="42703")
        { throw new InvalidOperationException("Solicite a aplicacao do script 20261123_entrada_bolsas.sql no banco deste exercicio.",e); }
    }
    internal static List<EntradaBolsaProduto> Consolidar(IReadOnlyList<EntradaBolsaItem> itens)
    {
        if(itens.Select(i=>i.Id).Distinct().Count()!=itens.Count)
            throw new InvalidOperationException("Consulta de produtos duplicada. Solicite verificacao ao suporte.");
        foreach(var i in itens)
        {
            if(i.Registrado<0 || i.Retornado<i.Registrado || i.Retornado<0 ||
                (i.Retornado>0 && (!i.Enviado.HasValue || i.Retornado>i.Enviado)))
                throw new InvalidOperationException($"Confira o retorno do item {i.Id}: abaixo do registrado ou acima do enviado.");
            if(i.Pendente>0 && (i.Produto==null || i.Descricao==null || i.Barcodes!=1 || string.IsNullOrWhiteSpace(i.Barcode)))
                throw new InvalidOperationException($"Confira produto e codigo de barras do item {i.Id}.");
        }
        var result=itens.Where(i=>i.Pendente>0).GroupBy(i=>i.Produto!.Value)
            .Select(g=>new EntradaBolsaProduto(g.Key,g.First().Descricao!,g.Sum(i=>i.Pendente))).ToList();
        if(result.Count==0) throw new InvalidOperationException("Nao ha novas quantidades de retorno para registrar no estoque.");
        return result;
    }
    internal async Task<int> Salvar(int funcionario,IReadOnlyList<EntradaBolsaItem> previa,
        string origem,string atendente,int os,string endereco,string estado)
    {
        if(!Origens.Contains(origem)||!Estados.Contains(estado)||string.IsNullOrWhiteSpace(atendente)||os<0||endereco.Length>12)
            throw new InvalidOperationException("Confira origem, atendente, estado, O.S. e endereco.");
        if(string.IsNullOrWhiteSpace(usuario)||usuario.Length>(origem=="ACERTO ESTOQUE"?25:50))
            throw new InvalidOperationException("Usuario configurado invalido.");
        await using var db=new NpgsqlConnection(conexao);
        await db.OpenAsync();
        await using var tx=await db.BeginTransactionAsync();
        await db.ExecuteAsync("LOCK TABLE almoxarifado_apoio.tblsaidabolsa IN SHARE ROW EXCLUSIVE MODE",transaction:tx);
        var atuais=(await db.QueryAsync<EntradaBolsaItem>(Sql,new {funcionario},tx)).ToList();
        if(!atuais.Select(i=>(i.Id,i.Versao)).SequenceEqual(previa.Select(i=>(i.Id,i.Versao))))
            throw new InvalidOperationException("As bolsas mudaram ou ja tiveram entrada em outra estacao. Reabra o formulario.");
        var produtos=Consolidar(atuais);
        if(!await db.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM almoxarifado_jac.tbl_atendentes_almox WHERE nome_funcionario=@atendente)",new{atendente},tx))
            throw new InvalidOperationException("Atendente nao encontrado.");
        var nomes=(await db.QueryAsync<string>("SELECT DISTINCT nome_apelido FROM almoxarifado_jac.view_funcionarios_terceiros WHERE codfun=@funcionario",new{funcionario},tx)).ToList();
        if(nomes.Count!=1) throw new InvalidOperationException("Funcionario nao encontrado ou ambiguo.");
        var movimento=await db.ExecuteScalarAsync<int>("SELECT nextval('almoxarifado_jac.seq_movimentacao_entrada')",transaction:tx);
        var data=DateTime.Today;
        foreach(var produto in produtos)
        {
            var item=atuais.First(i=>i.Produto==produto.Produto);
            var entrada=await db.ExecuteScalarAsync<int>("""
                INSERT INTO almoxarifado_jac.t_entrada_almox
                (cod_movimentacao,barcode,qtde,data,resp,requisicao,origem,incluido_por,incluido_data,
                 estado,funcionario,unidade,num_os,endereco,codcompladicional,codfun,obs)
                VALUES (@movimento,@Barcode,@Quantidade,@data,@atendente,0,@origem,@usuario,LOCALTIMESTAMP,
                 @estado,@nome,@Unidade,@os,@endereco,@Produto,@funcionario,'RETORNO CONSOLIDADO DE BOLSAS')
                RETURNING cod_entrada_almox
                """,new{movimento,item.Barcode,produto.Quantidade,data,atendente,origem,usuario,estado,
                    nome=nomes[0],item.Unidade,os,endereco,produto.Produto,funcionario},tx);
            if(origem=="ACERTO ESTOQUE")
                await db.ExecuteAsync("""
                    INSERT INTO pcp.tbl_controle_almox_estoque
                    (cod_movimentacao,cod_almox,barcode,codcompladicional,quantidade,data,operacao,processo,
                     num_os,incluido_por,incluido_data,bloqueado)
                    VALUES (@movimento,@entrada,@Barcode,@Produto,@Quantidade,@data,@origem,'ENTRADA',@os,@usuario,@data,'-1')
                    """,new{movimento,entrada,item.Barcode,produto.Produto,produto.Quantidade,data,origem,os,usuario},tx);
            foreach(var linha in atuais.Where(i=>i.Produto==produto.Produto&&i.Pendente>0))
            {
                await db.ExecuteAsync("""
                    INSERT INTO almoxarifado_apoio.tblbolsa_entradas_estoque
                    (cod_controlelinha,cod_entrada_almox,quantidade,registrado_por)
                    VALUES (@Id,@entrada,@Pendente,@usuario)
                    """,new{linha.Id,entrada,linha.Pendente,usuario},tx);
                await db.ExecuteAsync("UPDATE almoxarifado_apoio.tblsaidabolsa SET entrada_estoque_qtde=@Retornado WHERE cod_controlelinha=@Id",
                    new{linha.Id,linha.Retornado},tx);
            }
        }
        await tx.CommitAsync();
        return movimento;
    }
}
