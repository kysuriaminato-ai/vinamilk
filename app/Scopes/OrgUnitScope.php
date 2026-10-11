<?php

namespace App\Scopes;

use Illuminate\Database\Eloquent\Builder;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Scope;
use Illuminate\Support\Facades\Auth;

class OrgUnitScope implements Scope
{
    /**
     * Áp dụng ràng buộc phạm vi dữ liệu cây tổ chức cho câu truy vấn Eloquent.
     */
    public function apply(Builder $builder, Model $model): void
    {
        // Bỏ qua lọc nếu đang chạy CLI/Queue job hoặc chưa đăng nhập
        if (!Auth::check()) {
            return;
        }

        $user = Auth::user();

        // Super Admin (Ban TCCB Tập đoàn) có quyền xem toàn bộ cây
        if ($user->hasRole('SUPER_ADMIN')) {
            return;
        }

        // Lấy danh sách phạm vi nút cây được giao cho người dùng (user_scopes)
        $userScopes = $user->scopes()->with('orgUnit')->get();

        if ($userScopes->isEmpty()) {
            // Nếu không được phân phạm vi nào, chặn toàn bộ dữ liệu
            $builder->whereRaw('1 = 0');
            return;
        }

        // Lọc nhân sự thuộc các nhánh cây con mà người dùng được phân quyền
        $builder->whereHas('currentAssignment.orgUnit', function (Builder $query) use ($userScopes) {
            $query->where(function (Builder $subQuery) use ($userScopes) {
                foreach ($userScopes as $scope) {
                    $orgUnit = $scope->orgUnit;
                    if (!$orgUnit) {
                        continue;
                    }

                    if ($scope->include_sub_units) {
                        // Kế thừa toàn bộ nhánh con: path LIKE '/1/4/%'
                        $subQuery->orWhere('path', 'LIKE', $orgUnit->path . '%');
                    } else {
                        // Chỉ đơn vị trực tiếp
                        $subQuery->orWhere('id', $orgUnit->id);
                    }
                }
            });
        });
    }
}
