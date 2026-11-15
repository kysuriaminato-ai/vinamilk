<?php
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

register_shutdown_function(function() {
    echo "SHUTDOWN. Headers: ";
    print_r(headers_list());
    echo "\nOutput buffer: " . ob_get_clean();
});

$_SERVER['REQUEST_URI'] = '/vinamilk/luong';
$_SERVER['REQUEST_METHOD'] = 'GET';
$_SERVER['SCRIPT_NAME'] = '/vinamilk/public/index.php';
session_start();
$_SESSION['user_id'] = 1;
$_SESSION['user'] = [
    'Username' => 'admin',
    'HoTen' => 'Admin',
    'Role' => 'Admin'
];
require 'C:\xampp\htdocs\vinamilk\public\index.php';
