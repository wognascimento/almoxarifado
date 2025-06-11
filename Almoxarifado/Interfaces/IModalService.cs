using Almoxarifado.DataBase.Model;
using System.Windows;
using System.Windows.Controls;

namespace Almoxarifado.Interfaces;

public interface IModalService
{
    public interface IModalService
    {
        void ShowModalAt(UserControl modalContent, Point position, Size size);
        void CloseModal();
    }

    public interface IProductSearchModal
    {
        event EventHandler<ProductSelectedEventArgs> ProductSelected;
        event EventHandler ModalClosed;
    }

    public class ProductSelectedEventArgs : EventArgs
    {
        public QryDescricaoModel? Product { get; set; }
        public long? ProductCode { get; set; }
    }
}
