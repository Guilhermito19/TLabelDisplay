try {

$src1 = "C:\Users\guilherme.tavares\Desktop\TDisplayLabel\TLabelDisplay\TLabelDisplay\obj\Debug\T.Portable.Controls.TLabelDisplay.dll"
$dst1 = "C:\Program Files (x86)\SPIN\Action.NetX\an-10\WpfControls\T.Portable.Controls.TLabelDisplay.dll"

$src2 = "C:\Users\guilherme.tavares\Desktop\TDisplayLabel\TLabelDisplay\TLabelDisplay.HTML5\bin\T.Portable.Controls.TLabelDisplay.dll"
$dst2 = "C:\Program Files (x86)\SPIN\Action.NetX\an-10\HTML5\ExtensionControls\T.Portable.Controls.TLabelDisplay.dll"

Copy-Item $src1 $dst1 -Force
Copy-Item $src2 $dst2 -Force

Write-Host "Copia concluida."

}
catch {
    Write-Host "Erro:"
    Write-Host $_
}

Write-Host ""
Write-Host "Pressione ENTER para fechar..."
Read-Host