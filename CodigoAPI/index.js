// index.js
const express = require('express');
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const db = require('./db');
const authenticateToken = require('./auth');
require('dotenv').config();

const app = express();
const PORT = process.env.PORT || 3000;

app.use(express.json());

// registro
app.post('/register', (req, res) => {
  const { username, password } = req.body;
  const hash = bcrypt.hashSync(password, 8);

  const query = 'INSERT INTO users (username, password) VALUES (?, ?)';
  db.run(query, [username, hash], function(err) {
    if (err) return res.status(400).json({ error: 'Usuário já existe' });
    res.json({ message: 'Usuário registrado com sucesso' });
  });
});

// login
app.post('/login', (req, res) => {
  const { username, password } = req.body;

  db.get('SELECT * FROM users WHERE username = ?', [username], (err, user) => {
    if (err || !user) return res.status(400).json({ error: 'Usuário não encontrado' });

    const isValid = bcrypt.compareSync(password, user.password);
    if (!isValid) return res.status(403).json({ error: 'Senha inválida' });

    const token = jwt.sign({ id: user.id }, process.env.JWT_SECRET, { expiresIn: '1h' });
    res.json({ token });
  });
});

// registrar pontuação
app.post('/score', authenticateToken, (req, res) => {
  const { score } = req.body;
  const userId = req.user.id;

  const query = 'INSERT INTO scores (user_id, score) VALUES (?, ?)';
  db.run(query, [userId, score], function(err) {
    if (err) return res.status(500).json({ error: 'Erro ao salvar pontuação' });
    res.json({ message: 'Pontuação salva com sucesso' });
  });
});

// listar pontuações
app.get('/scores', authenticateToken, (req, res) => {
  const userId = req.user.id;

  db.all('SELECT score, created_at FROM scores WHERE user_id = ?', [userId], (err, rows) => {
    if (err) return res.status(500).json({ error: 'Erro ao buscar pontuações' });
    res.json({ scores: rows });
  });
});

app.listen(PORT, () => {
  console.log(`Servidor rodando em http://localhost:${PORT}`);
});