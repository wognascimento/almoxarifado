using Almoxarifado.DataBase.Model;
using Microsoft.EntityFrameworkCore;

namespace Almoxarifado.DataBase
{
    public class DatabaseContext : DbContext
    {
        private DataBaseSettings BaseSettings = DataBaseSettings.Instance;
        static DatabaseContext() => AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(
                $"host={BaseSettings.Host};" +
                $"user id={BaseSettings.Username};" +
                $"password={BaseSettings.Password};" +
                $"database={BaseSettings.Database};" +
                $"Application Name=SIG Almoxarifado <{BaseSettings.Database}>;",
                options => { options.EnableRetryOnFailure(); }
                );
            optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public DbSet<AtendenteAlmoxModel> AtendentesAlmox { get; set; }
        public DbSet<FuncionarioModel> Funcionarios { get; set; }
        public DbSet<ProdutoAlmoxModel> ProdutosAlmox { get; set; }
        public DbSet<ControleAlmoxEstoqueModel> ControleAlmoxEstoques { get; set; }
        public DbSet<SaidaAlmoxModel> Saidas { get; set; }

        public DbSet<RelplanModel> Relplans { get; set; }
        public DbSet<ProdutoModel> Produtos { get; set; }
        public DbSet<TabelaDescAdicionalModel> DescAdicionais { get; set; }
        public DbSet<TblComplementoAdicionalModel> ComplementoAdicionais { get; set; }
        public DbSet<QryDescricaoModel> Descricoes { get; set; }
        public DbSet<BarcodeModel> Barcodes { get; set; }
    }
}
