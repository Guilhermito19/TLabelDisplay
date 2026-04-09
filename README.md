# TLabelDisplay — Componente Portable Didático para SCADA

Componente minimalista criado para ilustrar a estrutura de um **portable control**
para SCADA com suporte a WPF (desktop) e HTML5 (browser via OpenSilver).

---

## O que o componente faz

Exibe o valor de uma **tag do SCADA** em um Label, com prefixo e sufixo opcionais.

```
[ Prefixo ]  [ valor da tag ]  [ Sufixo ]
   "Temp:"       "42.5"           "°C"
        →  exibe: "Temp: 42.5°C"
```

---

## Estrutura da Solution

```
TLabelDisplay.sln
│
├── TLabelDisplay/                   ← Projeto WPF (.NET 4.8)
│   ├── TLabelDisplay.cs             ← ★ ARQUIVO PRINCIPAL (compartilhado)
│   ├── TLabelDisplayConfig.xaml     ← Painel de config do designer
│   ├── TLabelDisplayConfig.xaml.cs
│   ├── TLabelDisplay.png            ← Ícone na toolbox
│   └── Properties/AssemblyInfo.cs
│
└── TLabelDisplay.HTML5/             ← Projeto OpenSilver (.NET Standard 2.0)
    └── TLabelDisplay.HTML5.csproj   ← Referencia o .cs via <Compile Link="...">
```

> O `TLabelDisplay.cs` **não está duplicado**. O projeto HTML5 compila o mesmo
> arquivo com o símbolo `OPENSILVER` definido, ativando os blocos `#if OPENSILVER`.

---

## Interfaces implementadas

| Interface              | Para que serve                                              |
|------------------------|-------------------------------------------------------------|
| `IPortableControl`     | Contrato principal com o SCADA                              |
| `INotifyPropertyChanged` | Reatividade de propriedades                               |
| `IControlConfig`       | Painel de configuração no designer (só no projeto WPF)      |
| `IOnOKMethod`          | Callback de confirmação do painel de config                 |

---

## Fluxo de vida do componente

```
Designer salva a tela
        ↓
SCADA carrega a tela
        ↓
StartRuntime()              ← registra escuta na tag
        ↓
Tag muda de valor
        ↓
HandleTagEvent()            ← lê novo valor, atualiza DisplayText
        ↓
TextBlock.Text atualizado   ← usuário vê o novo valor
        ↓
Tela fechada
        ↓
Dispose()                   ← cancela escuta, libera memória
```

---

## Como adicionar um novo componente portable

Copie esta estrutura e:

1. Renomeie os arquivos e namespaces
2. Mantenha as interfaces `IPortableControl` e `INotifyPropertyChanged`
3. Implemente `StartRuntime()` para registrar a escuta de tags
4. Implemente `HandleTagEvent()` para reagir às mudanças
5. Use `#if OPENSILVER` onde o comportamento difere entre WPF e HTML5
6. Lembre-se do `Dispose()` para cancelar os eventos registrados
