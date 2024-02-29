using Almoxarifado.DataBase.Model;
using Syncfusion.UI.Xaml.Grid;

namespace Almoxarifado.Custom
{
    public class CustomMultiColumnControl : SfMultiColumnDropDownControl
    {
        /// <summary>
        /// Returns true if the item is displayed in the Filtered List, otherwise returns false.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>

        protected override bool FilterRecord(object item)
        {
            var _item = item as FuncionarioModel;
            var result = (_item.nome_apelido.Contains(this.SearchText)) || (_item.codfun.ToString().Contains(this.SearchText));
            return result;
        }
    }
}

