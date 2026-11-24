using Almoxarifado.Views.Movimentacoes;

static BaixaBolsaItem Item(int id, int produto, double quantidade) => new()
{ Id=id, Produto=produto, Quantidade=quantidade, Descricao="Produto teste", Barcode="123", Barcodes=1 };
static void Check(bool ok, string texto)
{ if(!ok) throw new Exception(texto); Console.WriteLine("OK: " + texto); }
static void Rejeitar(List<BaixaBolsaItem> itens, string texto)
{
    try { BaixaBolsaService.Consolidar(itens); }
    catch(InvalidOperationException) { Console.WriteLine("OK: " + texto); return; }
    throw new Exception(texto);
}
var dados = new List<BaixaBolsaItem> { Item(1,10,2),Item(2,10,3),Item(3,20,1.5),Item(4,30,0) };
var produtos = BaixaBolsaService.Consolidar(dados);
Check(produtos.Count==2 && produtos.Single(i=>i.Produto==10).Quantidade==5,
    "Produtos repetidos em bolsas diferentes sao somados uma vez");
Check(produtos.Single(i=>i.Produto==20).Quantidade==1.5,"Quantidades fracionadas preservadas");
Rejeitar([],"Sem pendencias nao gera baixa");
Rejeitar([Item(1,10,0)],"Sem quantidade positiva nao gera saida");
Rejeitar([Item(1,10,-1)],"Quantidade negativa bloqueada");
Rejeitar([Item(1,10,double.NaN)],"Quantidade nao numerica bloqueada");
Rejeitar([Item(1,10,1),Item(1,10,1)],"Join duplicado nao multiplica estoque");
var retorno=Item(1,10,1); retorno.Retornado=1;
Rejeitar([retorno],"Devolucao anterior exige conferencia");
var semCodigo=Item(1,10,1); semCodigo.Barcodes=0;
Rejeitar([semCodigo],"Codigo de barras ausente bloqueado");
var ambiguo=Item(1,10,1); ambiguo.Barcodes=2;
Rejeitar([ambiguo],"Codigo de barras ambiguo bloqueado");
Check(BaixaBolsaService.Destinos.Contains("ACERTO ESTOQUE"),"Destinos seguem a saida normal");

static EntradaBolsaItem Retorno(int id,int produto,decimal enviado,decimal retorno,decimal registrado) => new()
{ Id=id,Produto=produto,Enviado=enviado,Retornado=retorno,Registrado=registrado,Descricao="Teste",Barcode="123",Barcodes=1 };
static void RejeitarEntrada(List<EntradaBolsaItem> itens,string texto)
{
    try { EntradaBolsaService.Consolidar(itens); }
    catch(InvalidOperationException) { Console.WriteLine("OK: "+texto); return; }
    throw new Exception(texto);
}
var parciais=new List<EntradaBolsaItem> { Retorno(1,10,8,5,3),Retorno(2,10,4,2,1),Retorno(3,20,3,3,3) };
var entrada=EntradaBolsaService.Consolidar(parciais);
Check(entrada.Count==1&&entrada[0].Quantidade==3,"Entrada soma somente diferencas de todas as bolsas");
foreach(var i in parciais) i.Registrado=i.Retornado;
RejeitarEntrada(parciais,"Repetir entrada sem novo retorno nao gera movimento");
parciais[0].Retornado=7;
Check(EntradaBolsaService.Consolidar(parciais).Single().Quantidade==2,"Segundo retorno parcial registra apenas incremento");
RejeitarEntrada([Retorno(1,10,5,6,0)],"Retorno acima da saida bloqueado");
RejeitarEntrada([Retorno(1,10,5,1,2)],"Reducao abaixo do estoque registrado bloqueada");
RejeitarEntrada([Retorno(1,10,5,-1,0)],"Retorno negativo bloqueado");
RejeitarEntrada([Retorno(1,10,5,1,0),Retorno(1,10,5,1,0)],"Duplicidade no join de entrada bloqueada");
Check(EntradaBolsaService.Consolidar([Retorno(1,10,1,0.3m,0.1m)]).Single().Quantidade==0.2m,
    "Diferencas fracionadas calculadas em decimal");
