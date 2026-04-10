// =============================================================================
// TLabelDisplay.cs — Componente Portable Didático para SCADA
// =============================================================================
//
// REFATORAÇÃO MVVM:
//   Antes: a UI era construída via código (new TextBlock(), this.Content = ...)
//   Agora: a UI está declarada no TLabelDisplay.xaml (XAML + bindings)
//          Este arquivo contém APENAS a lógica — funciona como ViewModel.
//
// PORTABLE:
//   O mesmo .cs é compilado pelos dois projetos:
//     1. TLabelDisplay/        → WPF (.NET 4.8)
//     2. TLabelDisplay.HTML5/  → OpenSilver (.NET Standard 2.0)
//
//   #if OPENSILVER  →  código exclusivo HTML5/browser
//   #else           →  código exclusivo WPF/desktop
//
// =============================================================================

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using T.Library;
using T.Toolkit;
using T.Toolkit.Wpf;

namespace T.Portable.Controls
{
    [System.Reflection.ObfuscationAttribute(Feature = "renaming", ApplyToMembers = true)]
    public partial class TLabelDisplay : UserControl, IPortableControl, INotifyPropertyChanged
    {

        // =====================================================================
        // INotifyPropertyChanged
        // =====================================================================

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        // =====================================================================
        // PROPRIEDADES BINDÁVEIS
        // O XAML faz binding direto nessas propriedades via {Binding ...}
        // =====================================================================

        // --- DisplayText ---
        // Texto atualmente exibido na View. Atualizado em runtime pelo HandleTagEvent().
        private string _displayText = "Inicializado padrão";
        public string DisplayText
        {
            get => _displayText;
            set
            {
                if (_displayText == value) return;
                _displayText = value;
                NotifyPropertyChanged();
            }
        }

        // --- LinkedValue ---
        // Campo que o usuário preenche no designer com o nome de uma tag/expressão.
        // Exemplo: "MyDevice.MyTag" ou "\"Valor fixo\""
        private ObjectReference _linkedValueRef = null;
        private string _linkedValue = "";

        [DefaultValue("")]
        public string LinkedValue
        {
            get => _linkedValue;
            set => _linkedValue = value;
        }

        // --- Prefix / Suffix ---
        [DefaultValue("")]
        public string Prefix { get; set; } = "";

        [DefaultValue("")]
        public string Suffix { get; set; } = "";

        // --- FontSize ---
        // Sobrescreve a propriedade herdada para expor via INPC ao binding
        private double _fontSize = 14;
        public new double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize == value) return;
                _fontSize = value;
                NotifyPropertyChanged();
            }
        }

        // --- TextColor ---
        // Cor do texto; bindada no XAML ao Foreground do TextBlock
        private Brush _textColor = new SolidColorBrush(Colors.Black);
        public Brush TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                NotifyPropertyChanged();
            }
        }

        public string[] ControlDataCSS => throw new NotImplementedException();
        public string[] ControlDataJS  => throw new NotImplementedException();


        // =====================================================================
        // CONSTRUTOR
        // InitializeComponent() carrega e conecta o TLabelDisplay.xaml.
        // Não há mais criação manual de TextBlock aqui.
        // =====================================================================

        public TLabelDisplay()
        {
            InitializeComponent();

            // DataContext aponta para si mesmo para que o XAML
            // encontre as propriedades via {Binding DisplayText} etc.
            this.DataContext = this;
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

            WK.EndInit(this);
        }


        // =====================================================================
        // Dispose
        // =====================================================================

        public void Dispose()
        {
            WK.Dispose(ref this._linkedValueRef);
        }


        // =====================================================================
        // IPortableControl
        // =====================================================================

        static public byte[] GetIconImage()
        {
            string uri = @"pack://application:,,,/T.Portable.Controls.TLabelDisplay;component/TLabelDisplay.png";
            return WK.GetImageFromUri(uri);
        }

        public IControlConfig GetConfigControl()
        {
#if OPENSILVER
            return null;
#else
            return new TLabelDisplayConfig();
#endif
        }

        // --- StartRuntime ---
        // Registra a escuta da tag. Não recria o TextBlock via código —
        // a View já existe graças ao XAML carregado no construtor.
        public void StartRuntime()
        {
            try
            {
                if (WK.IsDesignMode) return;

                string resolvedTagName = WK.UntokenizeObjectName(this._linkedValue.Trim());
                this._linkedValueRef   = ObjectReference.ParseString(resolvedTagName);

                if (!WK.IsUpdatingReport && this._linkedValueRef != null)
                    this._linkedValueRef.RegisterEvent(this.HandleTagEvent);

                ObjectReference.AddEventToExecute(this.HandleTagEvent);
            }
            catch (Exception ex)
            {
                TException.Log(ex);
            }
        }

        // --- HandleTagEvent ---
        public async Task HandleTagEvent()
        {
            string rawValue = "";

            if (this._linkedValueRef != null)
            {
                object val = await this._linkedValueRef.GetValueAsync(false);
                rawValue   = TConvert.ToString(val) ?? "";
            }

            string finalValue = $"{Prefix}{rawValue}{Suffix}";

#if OPENSILVER
            DisplayText = finalValue;
#else
            UpdateUI(() => DisplayText = finalValue);
#endif
        }

        // --- Labels / Tokens / Strings ---
        public void GetLabels(object symbol)    => WK.GetLabel(symbol, LinkedValue, eFieldTypeTk.StringWithExpressions);
        public void ResolveLabels(object symbol) => WK.ResolveLabel(symbol, ref this._linkedValue, eFieldTypeTk.StringWithExpressions);

        public void GetTokens(object symbol)    => WK.GetTokens(symbol, LinkedValue, eFieldTypeTk.StringWithExpressions);
        public void ApplyTokens(object symbol)  => LinkedValue = WK.ApplyTokens(symbol, LinkedValue, eFieldTypeTk.StringWithExpressions);

        public void GetStrings(object replaceControl)   { }
        public void ApplyStrings(object replaceControl) { }

        // --- Clipboard ---
        public void OnFinishClipboardCopy()  => LinkedValue = WK.OnFinishClipboardCopy(LinkedValue);
        public void OnFinishClipboardPaste() => LinkedValue = WK.OnFinishClipboardPaste(LinkedValue);
        public void OnStartClipboardCopy()   => LinkedValue = WK.OnStartClipboardCopy(LinkedValue);

        // --- OnSave ---
        public void OnSave(int displayId, object el)
        {
            this.OnStartClipboardCopy();
            this.OnFinishClipboardCopy();

            WK.SaveHelper.SaveStart(displayId, el);
            WK.SaveHelper.SaveLink(LinkedValue, "LinkedValue");
        }

#if !OPENSILVER
        public void UpdateUI(Action action)
            => Application.Current.Dispatcher.BeginInvoke(action);
#endif
    }
}
