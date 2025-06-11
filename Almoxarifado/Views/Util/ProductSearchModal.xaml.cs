using Almoxarifado.DataBase.Model;
using Almoxarifado.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using static Almoxarifado.Interfaces.IModalService;

namespace Almoxarifado.Views.Util;

/// <summary>
/// Interação lógica para ProductSearchModal.xam
/// </summary>
public partial class ProductSearchModal : UserControl, IProductSearchModal
{

    public event EventHandler<ProductSelectedEventArgs> ProductSelected;
    public event EventHandler ModalClosed;

    private readonly IProductService _productService;
    private List<QryDescricaoModel> _allProducts;
    private List<QryDescricaoModel> _filteredProducts;
    private bool _isLoading = false;

    public ProductSearchModal()
    {
        InitializeComponent();
        DataContext = this; // Para o binding do IsBusy
        _productService = new ProductService();
        InitializeModal();
    }

    private async void InitializeModal()
    {
        try
        {
            await LoadProductsAsync();
            TxtSearch.Focus();
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao carregar produtos: {ex.Message}");
        }
    }

    private async Task LoadProductsAsync()
    {
        if (_isLoading) return;

        _isLoading = true;
        UpdateStatus("Carregando produtos...");

        try
        {
            _allProducts = await _productService.GetProductsAsync();
            _filteredProducts = _allProducts.ToList();

            ProductsGrid.ItemsSource = _filteredProducts;
            UpdateStatus($"{_filteredProducts.Count} produtos encontrados");
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao carregar produtos: {ex.Message}");
            _allProducts = new List<QryDescricaoModel>();
            _filteredProducts = new List<QryDescricaoModel>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Filtrar com delay para melhor performance
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };

        timer.Tick += (s, args) =>
        {
            timer.Stop();
            FilterProducts();
        };

        timer.Start();
    }

    private void TxtSearch_QuerySubmitted(object sender, Telerik.Windows.Controls.AutoSuggestBox.QuerySubmittedEventArgs e)
    {
        FilterProducts();
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        FilterProducts();
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        TxtSearch.Text = "";
        FilterProducts();
        TxtSearch.Focus();
    }

    private void FilterProducts()
    {
        if (_allProducts == null || _isLoading) return;

        var searchText = TxtSearch.Text?.Trim().ToLower() ?? "";

        if (string.IsNullOrEmpty(searchText))
        {
            _filteredProducts = _allProducts.ToList();
        }
        else
        {
            _filteredProducts = _allProducts.Where(p =>
                p.descricao_completa.ToLower().Contains(searchText) ||
                p.unidade.ToLower().Contains(searchText)
            ).ToList();
        }

        ProductsGrid.ItemsSource = _filteredProducts;
        UpdateStatus($"{_filteredProducts.Count} produto(s) encontrado(s)");

        BtnSelect.IsEnabled = false;
    }

    private void ProductsGrid_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
    {
        BtnSelect.IsEnabled = ProductsGrid.SelectedItem != null;
    }

    private void ProductsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProductsGrid.SelectedItem != null)
        {
            SelectProduct();
        }
    }

    private void BtnSelect_Click(object sender, RoutedEventArgs e)
    {
        SelectProduct();
    }

    private void SelectProduct()
    {
        if (ProductsGrid.SelectedItem is QryDescricaoModel selectedProduct)
        {
            var args = new ProductSelectedEventArgs
            {
                Product = selectedProduct,
                ProductCode = selectedProduct.coduniadicional
            };

            ProductSelected?.Invoke(this, args);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        ModalClosed?.Invoke(this, EventArgs.Empty);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        ModalClosed?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateStatus(string message)
    {
        TxtStatus.Text = message;
    }

    private void ShowError(string message)
    {
        RadWindow.Alert(new DialogParameters
        {
            Content = message,
            Header = "Erro",
            IconContent = "⚠️"
        });
    }

    // Tratamento de teclas
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Escape:
                ModalClosed?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                break;

            case Key.Enter:
                if (ProductsGrid.SelectedItem != null && BtnSelect.IsEnabled)
                {
                    SelectProduct();
                    e.Handled = true;
                }
                break;

            case Key.F5:
                _ = LoadProductsAsync();
                e.Handled = true;
                break;
        }
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        TxtSearch.Focus();
    }

    private void TxtSearch_TextChanged(object sender, Telerik.Windows.Controls.AutoSuggestBox.TextChangedEventArgs e)
    {

    }
}