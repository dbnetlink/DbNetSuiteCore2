Get-ChildItem -Path . -Filter payment_p2007_*.csv |
Sort-Object Name |
ForEach-Object {
Get-Content $_
""
} | Out-File -FilePath "payment.csv" -Encoding UTF8