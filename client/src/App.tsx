import { Routes, Route } from 'react-router-dom';
import Typography from '@mui/material/Typography';

function HomePage() {
  return <Typography variant="h4">Dental Management</Typography>;
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
    </Routes>
  );
}

export default App;
