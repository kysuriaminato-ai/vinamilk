<?php
/**
 * PHPUnit Bootstrap File for Vinamilk HRM Test Suite
 */

declare(strict_types=1);

define('VINAMILK_HRM_TEST', true);
define('ROOT_DIR', dirname(__DIR__));

// Start session if not started
if (session_status() === PHP_SESSION_NONE) {
    session_start();
}

// Require database & core classes
require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/helpers/functions.php';
require_once ROOT_DIR . '/app/helpers/Auth.php';
require_once ROOT_DIR . '/app/core/Database.php';
require_once ROOT_DIR . '/app/core/Session.php';
require_once ROOT_DIR . '/app/core/Model.php';
require_once ROOT_DIR . '/app/core/Controller.php';

// Require key services
require_once ROOT_DIR . '/app/services/PayrollService.php';
require_once ROOT_DIR . '/app/services/CertificateService.php';
require_once ROOT_DIR . '/app/services/AssetPpeService.php';
require_once ROOT_DIR . '/app/services/OffboardingService.php';
require_once ROOT_DIR . '/app/services/LeaveService.php';

// Require key models
require_once ROOT_DIR . '/app/Models/EmployeeModel.php';
require_once ROOT_DIR . '/app/Models/ContractModel.php';
require_once ROOT_DIR . '/app/Models/AttendanceModel.php';
require_once ROOT_DIR . '/app/Models/PayrollModel.php';
require_once ROOT_DIR . '/app/Models/OrgUnitModel.php';
