// =============================================================================
// TLabelDisplayConfig.xaml.cs — Painel de Configuração do Componente
// =============================================================================
//
// Este arquivo é o code-behind do painel de configuração que aparece no
// designer do SCADA quando o usuário edita as propriedades do TLabelDisplay.
//
// INTERFACES IMPLEMENTADAS:
//   IControlConfig  → obrigatória para painéis de configuração de componentes
//   IOnOKMethod     → o SCADA chama OnOK() quando o usuário clica em "OK"
//
// FLUXO DE USO NO DESIGNER:
//   1. Usuário clica no componente na tela
//   2. SCADA chama GetConfigControl() no TLabelDisplay → retorna este objeto
//   3. SCADA chama LoadControl(componenteInstance) → preenchemos os campos
//   4. Usuário edita os campos e clica OK
//   5. SCADA chama OnOK() → aplicamos os valores de volta ao componente
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
        // =====================================================================
        // Metadados do painel — usados pelo SCADA para montar a janela de diálogo
        // =====================================================================

        public string title   = "Configuração do Label Display";
        public string Buttons = "OK";                  // botões exibidos na janela
        public eDialogSize Size = eDialogSize.Small;   // tamanho da janela

        // Referência para o componente sendo configurado
        private TLabelDisplay _controlRef = null;


        public TLabelDisplayConfig()
        {
            InitializeComponent();
        }


        // =====================================================================
        // IControlConfig.LoadControl
        // Chamado pelo SCADA para passar a instância do componente.
        // Aqui lemos as propriedades atuais e preenchemos os campos do painel.
        // =====================================================================

        public void LoadControl(object control)
        {
            this._controlRef = control as TLabelDisplay;
            if (this._controlRef == null) return;

            // Configura o FieldEditor para aceitar strings com expressões/tags
            this.Editor_LinkedValue.EditInfo = EditInfo.StringWithExpressions;

            // Preenche os campos com os valores atuais do componente
            this.Editor_LinkedValue.Text = this._controlRef.LinkedValue;
            this.Editor_Prefix.Text      = this._controlRef.Prefix;
            this.Editor_Suffix.Text      = this._controlRef.Suffix;
        }


        // =====================================================================
        // IOnOKMethod.OnOK
        // Chamado pelo SCADA quando o usuário confirma as alterações.
        // Aqui lemos os campos e aplicamos de volta ao componente.
        // Retornar false cancela o fechamento da janela (útil para validações).
        // =====================================================================

        public bool OnOK()
        {
            if (this._controlRef == null) return true;

            this._controlRef.LinkedValue = this.Editor_LinkedValue.Text;
            this._controlRef.Prefix      = this.Editor_Prefix.Text;
            this._controlRef.Suffix      = this.Editor_Suffix.Text;

            return true; // true = fecha a janela; false = mantém aberta
        }


        // =====================================================================
        // Tradução automática da UI ao carregar (multilíngue do SCADA)
        // =====================================================================

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TLocale.TranslateUI(this);
        }
    }
}
