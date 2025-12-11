import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import Login from './Login.jsx';
import Register from './Register.jsx';
import Todos from './Todos.jsx';
import './App.css';

function App() {

  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/" element={<Todos />} />
    </Routes>

  );
}

export default App;