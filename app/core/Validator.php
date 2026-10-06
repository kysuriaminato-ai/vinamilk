<?php

namespace App\Core;

class Validator
{
    private array $data;
    private array $errors = [];

    public function __construct(array $data)
    {
        $this->data = $data;
    }

    /**
     * Xác thực dữ liệu với các quy tắc truyền vào
     * $rules = ['email' => 'required|email', 'password' => 'required|min:8']
     */
    public function validate(array $rules): bool
    {
        foreach ($rules as $field => $ruleString) {
            $value = $this->data[$field] ?? null;
            $ruleArray = explode('|', $ruleString);

            foreach ($ruleArray as $rule) {
                if ($rule === 'required' && empty($value)) {
                    $this->addError($field, "Trường này là bắt buộc.");
                }

                if ($rule === 'email' && !empty($value) && !filter_var($value, FILTER_VALIDATE_EMAIL)) {
                    $this->addError($field, "Định dạng email không hợp lệ.");
                }

                if (strpos($rule, 'min:') === 0 && !empty($value)) {
                    $min = (int) str_replace('min:', '', $rule);
                    if (strlen($value) < $min) {
                        $this->addError($field, "Độ dài tối thiểu là {$min} ký tự.");
                    }
                }
                
                if ($rule === 'date' && !empty($value)) {
                    $d = \DateTime::createFromFormat('Y-m-d', $value);
                    if (!$d || $d->format('Y-m-d') !== $value) {
                         $this->addError($field, "Phải đúng định dạng ngày YYYY-MM-DD.");
                    }
                }
            }
        }

        return empty($this->errors);
    }

    private function addError(string $field, string $message): void
    {
        // Tránh ghi đè lỗi trước đó của cùng một trường
        if (!isset($this->errors[$field])) {
            $this->errors[$field] = $message;
        }
    }

    public function getErrors(): array
    {
        return $this->errors;
    }
}
