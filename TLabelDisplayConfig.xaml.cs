// =============================================================================
// TLabelDisplayConfig.xaml.cs — Painel de Configuração do Componente
// =============================================================================
//
// Sem mudanças em relação à versão original.
//
// O painel de config é um FORMULÁRIO PONTUAL — lê e escreve os valores
// manualmente em LoadControl/OnOK. Não usa binding reativo porque não precisa:
// o usuário edita, clica OK, e o SCADA aplica as mudanças de uma vez.
//
// =============================================================================

using System.Windows;
using System.Windows.Controls;
using T.Library;
using T.Toolkit;
using T.Toolkit.Wpf;
using T.Wpf.Base;

namespace T.Portable.Controls
{
    [System.Reflection.ObfuscationAttribute(Feature = "renaming", ApplyToMembers = true)]
    public partial class TLabelDisplayConfig : UserControl, IControlConfig, IOnOKMethod
    {
        public string title   = "Configuração do Label Display";
        public string Buttons = "OK";
        public eDialogSize Size = eDialogSize.Small;

        private TLabelDisplay _controlRef = null;

        public TLabelDisplayConfig()
        {
            InitializeComponent();
        }

        // Chamado pelo SCADA para passar a instância do componente sendo configurado.
        public void LoadControl(object control)
        {
            this._controlRef = control as TLabelDisplay;
            if (this._controlRef == null) return;

            this.Editor_LinkedValue.EditInfo = EditInfo.StringWithExpressions;

            this.Editor_LinkedValue.Text = this._controlRef.LinkedValue;
            this.Editor_Prefix.Text      = this._controlRef.Prefix;
            this.Editor_Suffix.Text      = this._controlRef.Suffix;
        }

        // Chamado pelo SCADA ao clicar OK — aplica os valores de volta ao componente.
        public bool OnOK()
        {
            if (this._controlRef == null) return true;

            this._controlRef.LinkedValue = this.Editor_LinkedValue.Text;
            this._controlRef.Prefix      = this.Editor_Prefix.Text;
            this._controlRef.Suffix      = this.Editor_Suffix.Text;

            return true;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TLocale.TranslateUI(this);
        }
    }
}
