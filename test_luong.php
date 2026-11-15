<?php
session_start();
$_SESSION['user_id'] = 1;
$_SESSION['role'] = 'Admin';
$_SESSION['username'] = 'admin';
$_SESSION['name'] = 'Admin';
$opts = array(
  'http'=>array(
    'method'=>"GET",
    'header'=>"Accept-language: en\r\n" .
              "Cookie: PHPSESSID=" . session_id() . "\r\n"
  )
);
$context = stream_context_create($opts);
$result = file_get_contents('http://localhost/vinamilk/luong', false, $context);
echo $result;
