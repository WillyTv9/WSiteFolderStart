<?php
session_start();

$errore = '';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $utente   = trim($_POST['utente'] ?? '');
    $password = $_POST['password'] ?? '';

    // TODO: verifica le credenziali (database + password_verify)
    $errore = 'Da completare: collega il controllo delle credenziali.';
}
?>
<!DOCTYPE html>
<html lang="it">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Login</title>
  <link rel="stylesheet" href="../Css/style.css">
</head>
<body>
  <main>
    <h1>Login</h1>
    <?php if ($errore): ?>
      <p><?= htmlspecialchars($errore) ?></p>
    <?php endif; ?>
    <form method="post">
      <input type="text" name="utente" placeholder="Utente" required>
      <input type="password" name="password" placeholder="Password" required>
      <button type="submit">Entra</button>
    </form>
  </main>
</body>
</html>
