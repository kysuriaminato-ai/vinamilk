<?php
// Parse schema from database/vinamilk_hrm.sql
$sqlContent = file_get_contents(__DIR__ . '/../database/vinamilk_hrm.sql');

$schemaTables = [];
if (preg_match_all('/CREATE TABLE `?([a-zA-Z0-9_]+)`?\s*\((.*?)\)\s*ENGINE/s', $sqlContent, $matches, PREG_SET_ORDER)) {
    foreach ($matches as $m) {
        $tblName = $m[1];
        $body = $m[2];
        $columns = [];
        $lines = explode("\n", $body);
        foreach ($lines as $line) {
            $line = trim($line);
            if (empty($line) || str_starts_with($line, '--')) continue;
            if (preg_match('/^`?([a-zA-Z0-9_]+)`?\s+([A-Za-z]+(?:\([^\)]+\))?)/', $line, $colMatch)) {
                $firstWord = strtoupper($colMatch[1]);
                if (!in_array($firstWord, ['PRIMARY', 'KEY', 'UNIQUE', 'CONSTRAINT', 'FOREIGN', 'CHECK', 'FULLTEXT', 'INDEX'])) {
                    $columns[$colMatch[1]] = $colMatch[2];
                }
            }
        }
        $schemaTables[$tblName] = $columns;
    }
}

echo "TOTAL TABLES IN database/vinamilk_hrm.sql: " . count($schemaTables) . "\n";
foreach ($schemaTables as $tbl => $cols) {
    echo "- $tbl (" . count($cols) . " columns): " . implode(', ', array_keys($cols)) . "\n";
}
