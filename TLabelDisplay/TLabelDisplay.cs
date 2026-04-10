// =============================================================================
// TLabelDisplay.cs — Componente Portable Didático para SCADA
// =============================================================================
//
// OBJETIVO: Mostrar o valor de uma tag do SCADA em um Label na tela.
//
// CONCEITO PORTABLE:
//   O mesmo arquivo .cs é compilado em dois projetos diferentes:
//     1. TLabelDisplay/          → WPF (.NET 4.8), roda no cliente desktop
//     2. TLabelDisplay.HTML5/    → OpenSilver (.NET Standard 2.0), roda no browser
//
//   A separação de código entre as plataformas é feita com:
//     #if OPENSILVER  →  código exclusivo para HTML5/browser
//     #else           →  código exclusivo para WPF/desktop
//     #endif
//
// INTERFACES OBRIGATÓRIAS (contrato com o SCADA):
//   IPortableControl       → reconhece o componente como um controle da tela
//   INotifyPropertyChanged → permite binding reativo de propriedades
//
// =============================================================================

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using T.Library;
using T.Toolkit;
using T.Toolkit.Wpf;

namespace T.Portable.Controls
{
    // ObfuscationAttribute: protege os nomes de membros contra engenharia reversa
    [System.Reflection.ObfuscationAttribute(Feature = "renaming", ApplyToMembers = true)]
    public class TLabelDisplay : UserControl, IPortableControl, INotifyPropertyChanged
    {

        // =====================================================================
        // INotifyPropertyChanged
        // Necessário para que o SCADA consiga observar mudanças de propriedades
        // =====================================================================

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        // =====================================================================
        // FIELDS — elementos visuais internos do controle
        // =====================================================================

        // O TextBlock é o elemento que de fato exibe o texto na tela
        private TextBlock textBlock = null;


        // =====================================================================
        // PROPRIEDADES CONFIGURÁVEIS PELO USUÁRIO
        // Aparecem no painel de propriedades do designer do SCADA
        // =====================================================================

        // --- LinkedValue ---
        // Campo que o usuário preenche no designer com o nome de uma tag ou expressão.
        // Exemplo: "MyDevice.MyTag" ou "\"Valor fixo\""
        // Durante o runtime, o SCADA monitora essa tag e chama HandleTagEvent()
        // sempre que o valor mudar.
        private ObjectReference _linkedValueRef = null;
        private string _linkedValue = "";

        [DefaultValue("")]
        public string LinkedValue
        {
            get => _linkedValue;
            set => _linkedValue = value;
        }

        // --- DisplayText ---
        // Texto atualmente exibido. Atualizado em runtime pelo HandleTagEvent().
        private string _displayText = "Inicializado padrão";
        public string DisplayText
        {
            get => _displayText;
            set
            {
                _displayText = value;
                NotifyPropertyChanged();

                // Seta diretamente — sem depender de binding
                if (this.textBlock == null)
                    return;

                this.textBlock.Text = value;
            }
        }

        // --- Prefix / Suffix ---
        // Textos opcionais que aparecem antes/depois do valor da tag.
        // Exemplo: Prefix="Temp: ", Suffix="°C" → exibe "Temp: 42.5°C"
        [DefaultValue("")]
        public string Prefix { get; set; } = "";

        [DefaultValue("")]
        public string Suffix { get; set; } = "";

        // --- FontSize da propriedade ---
        // Sobrescreve a propriedade herdada para também aplicar no TextBlock interno
        private double _fontSize = 14;
        public new double FontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                if (this.textBlock != null)
                    this.textBlock.FontSize = value;
            }
        }

        // --- TextColor ---
        // Cor do texto exibido
        private Brush _textColor = new SolidColorBrush(Colors.Black);
        public Brush TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                if (this.textBlock != null)
                    this.textBlock.Foreground = value;
            }
        }

        public string[] ControlDataCSS => throw new NotImplementedException();

        public string[] ControlDataJS => throw new NotImplementedException();


        // =====================================================================
        // CONSTRUTOR
        // Monta a árvore visual do controle
        // =====================================================================

        public TLabelDisplay()
        {
            DisplayText = "Inicializado no Constructor";

            this.DataContext = this;

            // Cria o TextBlock que exibirá o valor
            this.textBlock = new TextBlock
            {
                Text = "Constructor2",
                FontSize = _fontSize,
                Foreground = _textColor,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            // Define o TextBlock como conteúdo visual do UserControl
            this.Content = this.textBlock;
            this.MinHeight = 20;
            this.MinWidth = 40;
        }


        // =====================================================================
        // EndInit — chamado pelo XAML engine após todas as propriedades serem setadas
        // =====================================================================

        private bool _endInitRan = false;

#if OPENSILVER
        public override void EndInit()
        {
#else
        public override void EndInit()
        {
            base.EndInit();
#endif
            if (_endInitRan) return;
            _endInitRan = true;

            // WK.EndInit: registra o controle no sistema do SCADA
            WK.EndInit(this);
        }


        // =====================================================================
        // Dispose — limpeza de recursos quando a tela é fechada
        // =====================================================================

        public void Dispose()
        {
            // Cancela a escuta da tag para evitar memory leak
            WK.Dispose(ref this._linkedValueRef);
        }


        // =====================================================================
        // IPortableControl — IMPLEMENTAÇÃO DO CONTRATO COM O SCADA
        // =====================================================================

        // --- GetIconImage ---
        // Retorna o ícone que aparece na toolbox do designer
        static public byte[] GetIconImage()
        {
            // Aponta para o recurso de imagem embutido no assembly
            string uri = @"pack://application:,,,/T.Portable.Controls.TLabelDisplay;component/TLabelDisplay.png";
            return WK.GetImageFromUri(uri);
        }

        // --- GetConfigControl ---
        // Retorna o painel de configuração que abre no designer ao clicar no componente.
        // No HTML5 retornamos null porque a configuração só existe no designer WPF.
        public IControlConfig GetConfigControl()
        {
#if OPENSILVER
            return null;
#else
            return new TLabelDisplayConfig();
#endif
        }

        // --- StartRuntime ---
        // Chamado pelo SCADA quando a tela entra em modo de execução (runtime).
        // Aqui registramos a escuta da tag vinculada.
        public void StartRuntime()
        {
            try
            {
                if (WK.IsDesignMode) return;

                // Resolve o nome da tag (pode conter tokens/expressões do SCADA)
                string resolvedTagName = WK.UntokenizeObjectName(this._linkedValue.Trim());

                // Cria a referência para a tag. ObjectReference é o mecanismo do
                // SCADA para se vincular a uma tag e receber notificações de mudança.
                this._linkedValueRef = ObjectReference.ParseString(resolvedTagName);

                // Registra o callback: HandleTagEvent será chamado sempre que
                // o valor da tag mudar
                if (!WK.IsUpdatingReport && this._linkedValueRef != null)
                    this._linkedValueRef.RegisterEvent(this.HandleTagEvent);

                // Executa uma leitura inicial para exibir o valor atual imediatamente
                ObjectReference.AddEventToExecute(this.HandleTagEvent);

                this.textBlock = new TextBlock
                {
                    Text = "StartRunTime",
                    FontSize = _fontSize,
                    Foreground = _textColor,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                this.Content = this.textBlock;
                this.MinHeight = 20;
                this.MinWidth = 40;
            }
            catch (Exception ex)
            {
                TException.Log(ex);
            }
        }

        // --- HandleTagEvent ---
        // Chamado automaticamente pelo SCADA toda vez que o valor da tag muda.
        // É async porque a leitura de tags pode envolver I/O.
        public async Task HandleTagEvent()
        {
            string rawValue = "";

            if (this._linkedValueRef != null)
            {
                object val = await this._linkedValueRef.GetValueAsync(false);
                rawValue = TConvert.ToString(val) ?? "";
            }
            string finalValue = $"{Prefix}{rawValue}{Suffix}";

#if OPENSILVER
            DisplayText = finalValue;
#else
            UpdateUI(() =>
            {
                DisplayText = finalValue;
            });
#endif
        }

        // --- GetLabels / ResolveLabels ---
        // Suporte a multilíngue: extrai e aplica strings traduzíveis
        public void GetLabels(object symbol)
        {
            WK.GetLabel(symbol, LinkedValue, eFieldTypeTk.StringWithExpressions);
        }

        public void ResolveLabels(object symbol)
        {
            WK.ResolveLabel(symbol, ref this._linkedValue, eFieldTypeTk.StringWithExpressions);
        }

        // --- GetTokens / ApplyTokens ---
        // Suporte a templates de tela (instâncias reutilizáveis com tokens substituíveis)
        // Exemplo: LinkedValue = "{#Device}.Temperature" → token {#Device} é substituído
        // pelo nome do dispositivo real quando a tela é instanciada
        public void GetTokens(object symbol)
        {
            WK.GetTokens(symbol, LinkedValue, eFieldTypeTk.StringWithExpressions);
        }

        public void ApplyTokens(object symbol)
        {
            LinkedValue = WK.ApplyTokens(symbol, LinkedValue, eFieldTypeTk.StringWithExpressions);
        }

        // --- GetStrings / ApplyStrings ---
        // Usado para exportação/importação de strings (ex: para tradução em planilha)
        public void GetStrings(object replaceControl) { }
        public void ApplyStrings(object replaceControl) { }

        // --- Clipboard helpers ---
        // Ajustam referências internas durante operações de copiar/colar na tela
        public void OnFinishClipboardCopy() => LinkedValue = WK.OnFinishClipboardCopy(LinkedValue);
        public void OnFinishClipboardPaste() => LinkedValue = WK.OnFinishClipboardPaste(LinkedValue);
        public void OnStartClipboardCopy() => LinkedValue = WK.OnStartClipboardCopy(LinkedValue);

        // --- OnSave ---
        // Chamado quando a tela é salva (ex: para relatórios ou snapshots)
        public void OnSave(int displayId, object el)
        {
            this.OnStartClipboardCopy();
            this.OnFinishClipboardCopy();

            WK.SaveHelper.SaveStart(displayId, el);
            WK.SaveHelper.SaveLink(LinkedValue, "LinkedValue");
        }

#if !OPENSILVER
        public void UpdateUI(Action action)
        {
            Application.Current.Dispatcher.BeginInvoke(action);
        }
#endif
    }
}
