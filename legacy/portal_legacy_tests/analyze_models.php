<?php
$files = glob(__DIR__ . '/../app/models/*.php');
foreach ($files as $file) {
    echo "========================================\n";
    echo "FILE: " . basename($file) . "\n";
    $content = file_get_contents($file);
    
    // Find $table property
    if (preg_match('/protected\s+(?:string\s+)?\$table\s*=\s*[\'"]([^\'"]+)[\'"]/', $content, $m)) {
        echo "Declared table: " . $m[1] . "\n";
    }
    
    // Find table occurrences in SQL
    preg_match_all('/(?:FROM|JOIN|INTO|UPDATE)\s+`?([a-zA-Z0-9_]+)`?/i', $content, $sqlMatches);
    $tables = array_unique($sqlMatches[1] ?? []);
    echo "SQL tables: " . implode(', ', $tables) . "\n";

    // Find columns in SELECT or INSERT
    preg_match_all('/(?:SELECT|SET|VALUES\s*\(|WHERE)\s+([^;\)]+)/i', $content, $colMatches);
    // Print short excerpt
    echo "Sample lines with SQL queries:\n";
    $lines = explode("\n", $content);
    foreach ($lines as $i => $line) {
        if (preg_match('/(SELECT|INSERT|UPDATE|DELETE|JOIN|FROM)\s+/i', $line)) {
            echo "  L" . ($i + 1) . ": " . trim($line) . "\n";
        }
    }
}
