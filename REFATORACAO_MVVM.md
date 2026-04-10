# TLabelDisplay — Refatoração MVVM

## O que mudou

### Antes (UI construída via código)

```csharp
// TLabelDisplay.cs — construtor
this.textBlock = new TextBlock { Text = "...", FontSize = _fontSize, ... };
this.Content   = this.textBlock;

// StartRuntime() também recriava o TextBlock
this.textBlock = new TextBlock { ... };
this.Content   = this.textBlock;

// Propriedades setavam diretamente no campo privado
set { _fontSize = value; if (textBlock != null) textBlock.FontSize = value; }
```

### Depois (MVVM com XAML)

**TLabelDisplay.xaml** — View declarativa com bindings:
```xml
<TextBlock
    Text="{Binding DisplayText}"
    FontSize="{Binding FontSize}"
    Foreground="{Binding TextColor}" ... />
```

**TLabelDisplay.cs** — apenas lógica + INPC:
```csharp
// Propriedades só disparam NotifyPropertyChanged — não tocam na UI
public string DisplayText
{
    get => _displayText;
    set { _displayText = value; NotifyPropertyChanged(); }
}
```

---

## Por que isso funciona para WPF e HTML5 (OpenSilver)

O `TLabelDisplay.xaml` é compilado nos **dois projetos**:

| Projeto | Engine XAML | Resultado |
|---------|-------------|-----------|
| `TLabelDisplay/` (WPF) | WPF XAML | Renderiza como WPF nativo |
| `TLabelDisplay.HTML5/` (OpenSilver) | OpenSilver XAML | Transpila para HTML/CSS |

Os **bindings `{Binding ...}`** funcionam identicamente nos dois porque:
- WPF e OpenSilver implementam a mesma interface `INotifyPropertyChanged`
- O mecanismo de binding resolve as propriedades pelo nome via reflection
- O `DataContext = this` no construtor é suficiente nos dois ambientes

---

## Estrutura de arquivos

```
TLabelDisplay/
├── TLabelDisplay.cs              ← Lógica + ViewModel (portable, compartilhado)
├── TLabelDisplay.xaml            ← View XAML (compilado nos dois projetos)
├── TLabelDisplayConfig.xaml      ← Painel de config do designer (só WPF)
└── TLabelDisplayConfig.xaml.cs   ← Code-behind do painel (sem mudanças)
```

### Como o projeto HTML5 referencia o XAML

No `.csproj` do OpenSilver, adicione o link para o XAML assim como já faz para o `.cs`:

```xml
<!-- TLabelDisplay.HTML5.csproj -->
<ItemGroup>
  <!-- .cs já existia como link -->
  <Compile Include="..\TLabelDisplay\TLabelDisplay.cs">
    <Link>TLabelDisplay.cs</Link>
  </Compile>

  <!-- NOVO: linka o XAML também -->
  <Page Include="..\TLabelDisplay\TLabelDisplay.xaml">
    <Link>TLabelDisplay.xaml</Link>
    <Generator>MSBuild:Compile</Generator>
    <SubType>Designer</SubType>
  </Page>
</ItemGroup>
```

---

## O que NÃO mudou

- `TLabelDisplayConfig` permanece **igual** — é um formulário pontual (lê/escreve
  manualmente em `LoadControl`/`OnOK`), não precisa de binding reativo.
- Todo o contrato com o SCADA (`IPortableControl`, `StartRuntime`, `HandleTagEvent`,
  `Dispose`, tokens, labels, etc.) permanece **idêntico**.
- Os blocos `#if OPENSILVER` continuam no lugar certo.

---

## Fluxo de vida (sem mudanças)

```
StartRuntime()         ← registra escuta na tag (não recria mais o TextBlock!)
        ↓
HandleTagEvent()       ← lê valor, seta DisplayText
        ↓
INPC dispara           ← WPF/OpenSilver propaga a mudança ao binding
        ↓
TextBlock.Text muda    ← View atualiza automaticamente
```
