$emojis = @("🥛", "👤", "🔒", "👁️", "🔑", "📊", "🏢", "👥", "🎯", "📋", "🎓", "📂", "⏱️", "💰", "🔔", "🚪", "⚙️", "✅", "🏃", "📈", "🔄", "⚠️", "ℹ️", "📍", "🎉", "🖨️", "⚲", "✏️", "🗑️", "🎖️", "🔍", "▶", "🏛️", "➕", "📭", "🔓", "💾", "►", "›", "✨", "📉", "🛡️", "👨‍💼", "👩‍💼", "💼", "🏥", "🎗️", "🌍", "🗂️", "📖", "🏫", "📝", "🏦", "💻", "🌐", "🗣️", "🪖", "🏆", "🏅", "💍", "🛕", "🔧", "📚", "🚩")
$files = Get-ChildItem -Path "c:\Users\MAi THU\OneDrive\HTTT QLNL\vinamilk" -Include *.js, *.html -Recurse

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    $newContent = $content
    foreach ($emoji in $emojis) {
        $newContent = $newContent.Replace($emoji, "")
    }
    # Also clean up any empty spans or spaces left behind (optional, just let it be)
    if ($content -ne $newContent) {
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8
        Write-Host "Cleaned: $($file.FullName)"
    }
}
Write-Host "Done!"
