<?php
/**
 * =====================================================================
 * VINAMILK HRM - QUẢN LÝ MENU THEO PHÂN QUYỀN (MENU HELPER)
 * =====================================================================
 * Sinh danh sách menu từ bảng `menu_items` và `role_permissions`.
 * Chỉ hiển thị những mục mà vai trò của người dùng được cấp phép xem.
 */

require_once __DIR__ . '/../core/Database.php';
require_once __DIR__ . '/Auth.php';

class MenuHelper
{
    /**
     * Lấy cây menu được phân quyền cho người dùng hiện tại
     */
    public static function getUserMenu(): array
    {
        if (!Auth::check()) {
            return [];
        }

        $user = Auth::user();
        $roleId = (int)($user['RoleID'] ?? 0);
        $roleCode = strtoupper(Auth::role());

        $db = Database::getInstance()->getConnection();

        // 1. Truy vấn các mục menu được phép truy cập
        $userPerms = Auth::permissions();
        $isFullAccess = $roleCode === 'ADMIN' || $roleCode === 'ADMINISTRATOR' || $roleCode === 'SYSTEMADMIN' || in_array('*', $userPerms, true);

        if ($isFullAccess) {
            $sql = "SELECT * FROM menu_items WHERE IsVisible = 1 ORDER BY OrderIndex ASC";
            $rawItems = $db->query($sql)->fetchAll(PDO::FETCH_ASSOC);
        } elseif (!empty($userPerms)) {
            // Hiển thị Menu PHP trực tiếp theo danh sách quyền trả về từ GET /api/v1/me của Web API .NET
            $sql = "SELECT * FROM menu_items WHERE IsVisible = 1 ORDER BY OrderIndex ASC";
            $allItems = $db->query($sql)->fetchAll(PDO::FETCH_ASSOC);
            $rawItems = array_filter($allItems, function($m) use ($userPerms) {
                $req = trim((string)($m['RequiredPermission'] ?? ''));
                if ($req === '') return true;
                foreach ($userPerms as $p) {
                    if (strcasecmp($p, $req) === 0) return true;
                    $parts = explode('.', $p);
                    if (strcasecmp($parts[0], $req) === 0) return true;
                    if (strcasecmp($p, $req . '.View') === 0) return true;
                }
                return false;
            });
        } else {
            $sql = "SELECT DISTINCT m.*
                    FROM menu_items m
                    WHERE m.IsVisible = 1
                      AND (
                        m.RequiredPermission = '' OR m.RequiredPermission IS NULL
                        OR EXISTS (
                            SELECT 1 
                            FROM role_permissions rp 
                            JOIN permissions p ON rp.PermissionID = p.PermissionID 
                            WHERE rp.RoleID = :role_id 
                              AND p.ModuleCode = m.RequiredPermission
                        )
                      )
                    ORDER BY m.OrderIndex ASC";
            $stmt = $db->prepare($sql);
            $stmt->execute([':role_id' => $roleId]);
            $rawItems = $stmt->fetchAll(PDO::FETCH_ASSOC);
        }

        // 2. Tách thành cây cha - con
        $itemsById = [];
        $childrenMap = [];

        foreach ($rawItems as $item) {
            $itemsById[$item['MenuID']] = $item;
            $itemsById[$item['MenuID']]['children'] = [];
            
            if (!empty($item['ParentID'])) {
                $childrenMap[$item['ParentID']][] = $item['MenuID'];
            }
        }

        // Gán các mục con vào mục cha
        foreach ($childrenMap as $parentId => $childIds) {
            if (isset($itemsById[$parentId])) {
                foreach ($childIds as $cid) {
                    $itemsById[$parentId]['children'][] = $itemsById[$cid];
                }
            }
        }

        // 3. Lọc danh sách menu gốc (Root)
        $tree = [];
        $currentUri = parse_url($_SERVER['REQUEST_URI'] ?? '', PHP_URL_PATH);
        $urlParam = $_GET['url'] ?? '';

        foreach ($itemsById as $id => $item) {
            if (empty($item['ParentID'])) {
                // Nếu mục cha không có link trực tiếp và không có con nào được hiển thị -> bỏ qua
                if (empty($item['Route']) && empty($item['children'])) {
                    continue;
                }

                // Xác định trạng thái active
                $isActive = false;
                if (!empty($item['Route'])) {
                    if (str_contains($currentUri, $item['Route']) || str_starts_with($urlParam, $item['Route'])) {
                        $isActive = true;
                    }
                }

                // Kiểm tra xem có mục con nào active không
                $hasActiveChild = false;
                foreach ($item['children'] as &$child) {
                    $childActive = false;
                    if (!empty($child['Route'])) {
                        if (str_contains($currentUri, $child['Route']) || str_starts_with($urlParam, $child['Route'])) {
                            $childActive = true;
                            $hasActiveChild = true;
                        }
                    }
                    $child['is_active'] = $childActive;
                }
                unset($child);

                $item['is_active'] = $isActive || $hasActiveChild;
                $item['has_active_child'] = $hasActiveChild;
                $tree[] = $item;
            }
        }

        return $tree;
    }
}
