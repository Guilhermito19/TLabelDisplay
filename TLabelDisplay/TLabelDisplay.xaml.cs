using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using T.Library;
using T.Toolkit;
using T.Toolkit.Wpf;

namespace T.Portable.Controls
{
    // ObfuscationAttribute: protege os nomes de membros contra engenharia reversa
    [System.Reflection.ObfuscationAttribute(Feature = "renaming", ApplyToMembers = true)]
    public partial class TLabelDisplay : UserControl, IPortableControl, INotifyPropertyChanged
    {
        private TextBlock textBlock = null;

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
                if (this.textBlockView == null)
                    return;

                this.textBlockView.Text = value;
            }
        }

        public TLabelDisplay()
        {
            DisplayText = "Inicializado no Constructor";

            this.DataContext = this;

            this.Content = new TextBlock
            {
                Text = "Component"
            };
        }

        public void StartRuntime()
        {
            try
            {
                if (WK.IsDesignMode) return;

                // Resolve o nome da tag (pode conter tokens/expressões do SCADA)
                string resolvedTagName = WK.UntokenizeObjectName(this._linkedValue.Trim());

                this._linkedValueRef = ObjectReference.ParseString(resolvedTagName);

                if (!WK.IsUpdatingReport && this._linkedValueRef != null)
                    this._linkedValueRef.RegisterEvent(this.HandleTagEvent);

                ObjectReference.AddEventToExecute(this.HandleTagEvent);
                Console.WriteLine("Passando pelo RunTime");
#if OPENSILVER
                UpdateUIHTML(() => { InitializeObjectsFromXaml(); });
#else
                InitializeObjectsFromXaml();
#endif
            }
            catch (Exception ex)
            {
                TException.Log(ex);
            }
        }

        public void InitializeObjectsFromXaml()
        {
            //InitializeComponent()
            if (_contentLoaded)
            {
                return;
            }
            Console.WriteLine("InitializeObjectsFromXaml");
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/T.Portable.Controls.TLabelDisplay;component/tlabeldisplay.xaml", System.UriKind.Relative);
            System.Windows.Application.LoadComponent(this, resourceLocater);
        }

        public async Task HandleTagEvent()
        {
            string rawValue = "";

            if (this._linkedValueRef != null)
            {
                object val = await this._linkedValueRef.GetValueAsync(false);
                rawValue = TConvert.ToString(val) ?? "";
            }
            string finalValue = $"{rawValue}";

#if OPENSILVER
            UpdateUIHTML(() =>
            {
                DisplayText = finalValue;
            });
#else
            UpdateUI(() =>
            {
                DisplayText = finalValue;
            });
#endif
        }



#if !OPENSILVER
        public void UpdateUI(Action action)
        {
            Application.Current.Dispatcher.BeginInvoke(action);
        }
#endif

#if OPENSILVER
        public void UpdateUIHTML(Action action)
        {
            Dispatcher.InvokeAsync(action);
        }
#endif

        #region Stuff
        // =====================================================================
        // INotifyPropertyChanged
        // Necessário para que o SCADA consiga observar mudanças de propriedades
        // =====================================================================

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        public string[] ControlDataCSS;

        public string[] ControlDataJS;

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
        #endregion
    }
}
