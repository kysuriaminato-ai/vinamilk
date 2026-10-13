<?php
/**
 * Thêm menu cho 3 module mới:
 * 1. Chứng chỉ & An toàn (ATTP) -> route 'certificates'
 * 2. Tài sản & Trang bị PPE -> route 'assets'
 * 3. Quy trình Thôi việc -> route 'offboarding'
 */
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();

$menus = [
    [
        'Title' => 'Chứng chỉ & An toàn (ATTP)',
        'Route' => 'certificates',
        'Icon'  => 'bi-award-fill',
        'ParentID' => 3,
        'OrderIndex' => 6,
        'RequiredPermission' => 'nhansu'
    ],
    [
        'Title' => 'Tài sản & Trang bị PPE',
        'Route' => 'assets',
        'Icon'  => 'bi-shield-check',
        'ParentID' => 3,
        'OrderIndex' => 7,
        'RequiredPermission' => 'nhansu'
    ],
    [
        'Title' => 'Quy trình Thôi việc',
        'Route' => 'offboarding',
        'Icon'  => 'bi-box-arrow-right',
        'ParentID' => 3,
        'OrderIndex' => 8,
        'RequiredPermission' => 'nhansu'
    ]
];

foreach ($menus as $m) {
    $stmt = $pdo->prepare("SELECT MenuID FROM menu_items WHERE Route = :route");
    $stmt->execute([':route' => $m['Route']]);
    $id = $stmt->fetchColumn();

    if (!$id) {
        $ins = $pdo->prepare("INSERT INTO menu_items (Title, Route, Icon, ParentID, OrderIndex, RequiredPermission, IsVisible, CreatedAt)
                              VALUES (:title, :route, :icon, :parent_id, :order_idx, :req_perm, 1, NOW())");
        $ins->execute([
            ':title' => $m['Title'],
            ':route' => $m['Route'],
            ':icon'  => $m['Icon'],
            ':parent_id' => $m['ParentID'],
            ':order_idx' => $m['OrderIndex'],
            ':req_perm'  => $m['RequiredPermission']
        ]);
        echo "Added menu: {$m['Title']} -> {$m['Route']}\n";
    } else {
        $upd = $pdo->prepare("UPDATE menu_items SET Title = :title, Icon = :icon, ParentID = :parent_id, OrderIndex = :order_idx, RequiredPermission = :req_perm, IsVisible = 1 WHERE Route = :route");
        $upd->execute([
            ':title' => $m['Title'],
            ':icon'  => $m['Icon'],
            ':parent_id' => $m['ParentID'],
            ':order_idx' => $m['OrderIndex'],
            ':req_perm'  => $m['RequiredPermission'],
            ':route' => $m['Route']
        ]);
        echo "Updated menu: {$m['Title']} -> {$m['Route']}\n";
    }
}

echo "Menu items sync completed successfully.\n";
